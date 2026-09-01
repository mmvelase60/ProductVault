using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize(Roles = "Admin"), Route("api/admin")]
public sealed class AdminApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IReadOnlyList<AdminUserResponse>> Users()
    {
        var users = await db.Users.AsNoTracking().OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.FirstName, user.Surname, user.UserName, user.Email, user.EmailConfirmed })
            .ToListAsync();
        var roles = await (from membership in db.UserRoles.AsNoTracking()
                           join role in db.Roles.AsNoTracking() on membership.RoleId equals role.Id
                           select new { membership.UserId, RoleName = role.Name! }).ToListAsync();
        var rolesByUser = roles.GroupBy(item => item.UserId).ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(item => item.RoleName).OrderBy(name => name).ToArray());
        return users.Select(user => new AdminUserResponse(user.Id, user.FirstName ?? string.Empty, user.Surname ?? string.Empty, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.EmailConfirmed, rolesByUser.GetValueOrDefault(user.Id, Array.Empty<string>()))).ToList();
    }
}

public sealed record AdminUserResponse(string Id, string FirstName, string Surname, string Username, string Email, bool EmailConfirmed, IReadOnlyList<string> Roles);
