using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using ProductVault.Services;

namespace ProductVault.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<IdentityUser> users,
    IConfiguration configuration,
    IApplicationEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<MessageResponse>> Register(RegisterRequest request)
    {
        var email = request.Email.Trim();
        var user = new IdentityUser { UserName = email, Email = email };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.ToDictionary(error => error.Code, error => error.Description) });

        try
        {
            await SendConfirmationEmailAsync(user);
            return Accepted(new MessageResponse("Account created. Check your email to confirm your address before signing in."));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Confirmation email could not be sent for a newly registered user.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new MessageResponse("Account created, but the confirmation email could not be sent. Check the email configuration, then request a new confirmation link."));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new MessageResponse("Email or password is incorrect."));

        if (!user.EmailConfirmed)
            return StatusCode(StatusCodes.Status403Forbidden,
                new MessageResponse("Confirm your email address before signing in.", "email_confirmation_required"));

        return Ok(CreateResponse(user));
    }

    [HttpPost("confirm-email")]
    public async Task<ActionResult<MessageResponse>> ConfirmEmail(ConfirmEmailRequest request)
    {
        var user = await users.FindByIdAsync(request.UserId);
        if (user is null)
            return BadRequest(new MessageResponse("This confirmation link is invalid or has expired."));

        var result = await users.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            return BadRequest(new MessageResponse("This confirmation link is invalid or has expired. Request a new link and try again."));

        return Ok(new MessageResponse("Email confirmed. You can now sign in."));
    }

    [HttpPost("resend-confirmation")]
    public async Task<ActionResult<MessageResponse>> ResendConfirmation(EmailRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is not null && !user.EmailConfirmed)
        {
            try
            {
                await SendConfirmationEmailAsync(user);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Confirmation email could not be resent.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new MessageResponse("The confirmation email could not be sent. Check the email configuration and try again."));
            }
        }

        return Ok(new MessageResponse("If an unconfirmed account exists for that address, a confirmation email has been sent."));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(EmailRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is not null && user.EmailConfirmed)
        {
            try
            {
                var token = await users.GeneratePasswordResetTokenAsync(user);
                var link = BuildFrontendLink("reset-password", user.Id, token);
                await SendEmailAsync(user.Email!, "Reset your ProductVault password", "Reset your password", link,
                    "Use the link below to choose a new ProductVault password. If you did not request this, you can safely ignore this email.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Password reset email could not be sent.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new MessageResponse("The password reset email could not be sent. Check the email configuration and try again."));
            }
        }

        return Ok(new MessageResponse("If a confirmed account exists for that address, a password reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<MessageResponse>> ResetPassword(ResetPasswordRequest request)
    {
        var user = await users.FindByIdAsync(request.UserId);
        if (user is null)
            return BadRequest(new MessageResponse("This password reset link is invalid or has expired."));

        var result = await users.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.ToDictionary(error => error.Code, error => error.Description) });

        return Ok(new MessageResponse("Password reset successfully. You can now sign in."));
    }

    private async Task SendConfirmationEmailAsync(IdentityUser user)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var link = BuildFrontendLink("confirm-email", user.Id, token);
        await SendEmailAsync(user.Email!, "Confirm your ProductVault email", "Confirm your email", link,
            "Thanks for creating a ProductVault account. Confirm your email address to activate secure access to your private workspace.");
    }

    private async Task SendEmailAsync(string recipient, string subject, string heading, string link, string copy)
    {
        var safeHeading = HtmlEncoder.Default.Encode(heading);
        var safeCopy = HtmlEncoder.Default.Encode(copy);
        var safeLink = HtmlEncoder.Default.Encode(link);
        var html = $"<h1>{safeHeading}</h1><p>{safeCopy}</p><p><a href=\"{safeLink}\">{safeHeading}</a></p><p>If the button does not open, copy this link into your browser:</p><p>{safeLink}</p>";
        await emailSender.SendAsync(recipient, subject, html, $"{copy}\n\n{link}");
    }

    private string BuildFrontendLink(string path, string userId, string token)
    {
        var baseUrl = emailOptions.Value.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/{path}?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
    }

    private AuthResponse CreateResponse(IdentityUser user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(8);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims, expires: expiresAt, signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, user.Email ?? string.Empty);
    }
}

public sealed record RegisterRequest([Required, EmailAddress] string Email, [Required, MinLength(8)] string Password);
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public sealed record EmailRequest([Required, EmailAddress] string Email);
public sealed record ConfirmEmailRequest([Required] string UserId, [Required] string Token);
public sealed record ResetPasswordRequest([Required] string UserId, [Required] string Token, [Required, MinLength(8)] string Password);
public sealed record MessageResponse(string Message, string? Code = null);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, string Email);
