using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProductVault.Models;
using ProductVault.Services;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize, Route("api/profile")]
public sealed class ProfileApiController(UserManager<ApplicationUser> users, IUsernameGenerator usernames, IAuditTrailService audit, IRefreshTokenService refreshTokens, ProductVault.Data.ApplicationDbContext db) : ControllerBase
{
    private string UserId => users.GetUserId(User)!;

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        return Ok(await ToResponseAsync(user));
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var firstName = request.FirstName.Trim();
        var surname = request.Surname.Trim();
        if (!firstName.Any(char.IsLetterOrDigit) || !surname.Any(char.IsLetterOrDigit))
            return BadRequest(new { errors = new { profile = "First name and surname must contain letters or numbers." } });

        user.FirstName = firstName;
        user.Surname = surname;
        user.UserName = await usernames.NextAsync(firstName, surname, user.Id);
        var update = await users.UpdateAsync(user);
        if (!update.Succeeded) return BadRequest(new { errors = update.Errors.ToDictionary(error => error.Code, error => error.Description) });

        audit.Record(UserId, UserId, "Updated", "Profile", user.Id, user.UserName ?? "Profile");
        await db.SaveChangesAsync();
        return Ok(await ToResponseAsync(user));
    }

    [HttpPost("change-password")]
    public async Task<ActionResult<MessageResponse>> ChangePassword(ChangePasswordRequest request)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.ToDictionary(error => error.Code, error => error.Description) });

        await refreshTokens.RevokeAllAsync(user.Id, HttpContext.RequestAborted);
        audit.Record(UserId, UserId, "Changed password", "Profile", user.Id, user.UserName ?? "Profile");
        await db.SaveChangesAsync();
        return Ok(new MessageResponse("Password changed. Please sign in again."));
    }

    private async Task<ProfileResponse> ToResponseAsync(ApplicationUser user) => new(
        user.FirstName ?? string.Empty,
        user.Surname ?? string.Empty,
        user.UserName ?? string.Empty,
        user.Email ?? string.Empty,
        (await users.GetRolesAsync(user)).ToArray());
}

public sealed record UpdateProfileRequest([Required, StringLength(100)] string FirstName, [Required, StringLength(100)] string Surname);
public sealed record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(8)] string NewPassword);
public sealed record ProfileResponse(string FirstName, string Surname, string Username, string Email, IReadOnlyList<string> Roles);
