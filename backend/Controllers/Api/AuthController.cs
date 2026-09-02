using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using ProductVault.Data;
using ProductVault.Services;
using ProductVault.Models;

namespace ProductVault.Controllers.Api;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(
    UserManager<ApplicationUser> users,
    IConfiguration configuration,
    IApplicationEmailSender emailSender,
    IEmailVerificationCodeService verificationCodes,
    IUsernameGenerator usernameGenerator,
    IRefreshTokenService refreshTokens,
    ApplicationDbContext db,
    RoleBootstrapper roleBootstrapper,
    IOptions<EmailOptions> emailOptions,
    IOptions<RefreshTokenOptions> tokenOptions,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<MessageResponse>> Register(RegisterRequest request)
    {
        var firstName = request.FirstName.Trim();
        var surname = request.Surname.Trim();
        if (!firstName.Any(char.IsLetterOrDigit) || !surname.Any(char.IsLetterOrDigit))
            return BadRequest(new { errors = new { Name = "First name and surname must contain letters or numbers." } });

        var email = request.Email.Trim();
        var username = await usernameGenerator.NextAsync(firstName, surname);
        var user = new ApplicationUser
        {
            FirstName = firstName,
            Surname = surname,
            UserName = username,
            Email = email
        };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.ToDictionary(error => error.Code, error => error.Description) });

        await users.AddToRoleAsync(user, RoleBootstrapper.UserRole);
        if (roleBootstrapper.IsConfiguredAdmin(email))
            await users.AddToRoleAsync(user, RoleBootstrapper.AdminRole);

        try
        {
            await SendVerificationCodeEmailAsync(user);
            return Accepted(new MessageResponse("Account created. Enter the verification code sent to your email before signing in."));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Verification code email could not be sent for a newly registered user.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new MessageResponse("Account created, but the verification code could not be sent. Check the email configuration, then request a new code."));
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
                new MessageResponse("Verify your email address before signing in.", "email_confirmation_required"));

        var issue = await refreshTokens.IssueAsync(user, HttpContext.RequestAborted);
        WriteSessionCookies(issue);
        return Ok(await CreateResponseAsync(user));
    }

    [HttpPost("verify-email-code")]
    public async Task<ActionResult<MessageResponse>> VerifyEmailCode(VerifyEmailCodeRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
            return BadRequest(new MessageResponse("The verification code is invalid or has expired."));

        if (user.EmailConfirmed)
            return Ok(new MessageResponse("Email already verified. You can sign in."));

        var result = await verificationCodes.VerifyAsync(user, request.Code);
        if (result is not EmailVerificationCodeResult.Success)
        {
            var message = result switch
            {
                EmailVerificationCodeResult.Expired => "This verification code has expired. Request a new code and try again.",
                EmailVerificationCodeResult.TooManyAttempts => "Too many incorrect attempts. Request a new verification code.",
                _ => "The verification code is incorrect. Try again."
            };
            return BadRequest(new MessageResponse(message));
        }

        user.EmailConfirmed = true;
        var update = await users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            var errors = string.Join("; ", update.Errors.Select(error => $"{error.Code}: {error.Description}"));
            logger.LogWarning("Identity could not confirm email for user {UserId}: {Errors}. Falling back to a targeted confirmation update for this verified legacy account.", user.Id, errors);

            // The one-time code has already been verified. This narrow fallback supports
            // legacy Identity rows whose unrelated username data no longer satisfies a
            // full UserManager update, without bypassing the code-verification gate.
            var changed = await db.Users
                .Where(candidate => candidate.Id == user.Id && !candidate.EmailConfirmed)
                .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.EmailConfirmed, true), HttpContext.RequestAborted);
            if (changed != 1)
            {
                logger.LogError("Email verification fallback did not update user {UserId}.", user.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new MessageResponse("Email verification could not be completed. Request a new code and try again."));
            }
        }

        return Ok(new MessageResponse("Email verified. You can now sign in."));
    }

    [HttpPost("resend-confirmation")]
    public async Task<ActionResult<MessageResponse>> ResendConfirmation(EmailRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is not null && !user.EmailConfirmed)
        {
            try
            {
                await SendVerificationCodeEmailAsync(user);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Verification code email could not be resent.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new MessageResponse("The verification code could not be sent. Check the email configuration and try again."));
            }
        }

        return Ok(new MessageResponse("If an unverified account exists for that address, a verification code has been sent."));
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

        await refreshTokens.RevokeAllAsync(user.Id, HttpContext.RequestAborted);
        return Ok(new MessageResponse("Password reset successfully. You can now sign in."));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh()
    {
        if (!TryGetSessionRequest(out var token, out var csrfToken))
            return Unauthorized(new MessageResponse("Your session has ended. Please sign in again."));

        var result = await refreshTokens.RotateAsync(token, csrfToken, HttpContext.RequestAborted);
        if (result is null)
        {
            ClearSessionCookies();
            return Unauthorized(new MessageResponse("Your session has ended. Please sign in again."));
        }

        WriteSessionCookies(result.Issue);
        return Ok(await CreateResponseAsync(result.User));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!TryGetSessionRequest(out var token, out var csrfToken))
            return NoContent();

        if (!await refreshTokens.RevokeAsync(token, csrfToken, HttpContext.RequestAborted))
            return BadRequest(new MessageResponse("The sign-out request could not be verified."));

        ClearSessionCookies();
        return NoContent();
    }

    private async Task SendVerificationCodeEmailAsync(ApplicationUser user)
    {
        var code = await verificationCodes.CreateAsync(user);
        var html = $"<h1>Verify your ProductVault email</h1><p>Enter this verification code in ProductVault:</p><p style=\"font-size:28px;font-weight:700;letter-spacing:6px\">{code}</p><p>This code expires in {emailOptions.Value.VerificationCodeLifetimeMinutes} minutes. If you did not create an account, you can safely ignore this email.</p>";
        await emailSender.SendAsync(user.Email!, "Your ProductVault verification code", html,
            $"Your ProductVault verification code is {code}. It expires in {emailOptions.Value.VerificationCodeLifetimeMinutes} minutes.");
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

    private async Task<AuthResponse> CreateResponseAsync(ApplicationUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(Math.Clamp(tokenOptions.Value.AccessTokenLifetimeMinutes, 5, 60));
        var roles = await users.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims, expires: expiresAt, signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, user.Email ?? string.Empty, roles.ToArray());
    }

    private bool TryGetSessionRequest(out string token, out string csrfToken)
    {
        token = Request.Cookies[AuthenticationCookieNames.RefreshToken] ?? string.Empty;
        csrfToken = Request.Headers[AuthenticationCookieNames.CsrfHeader].ToString();
        return !string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(csrfToken);
    }

    private void WriteSessionCookies(RefreshTokenIssue issue)
    {
        var expiresAt = new DateTimeOffset(issue.ExpiresAt);
        Response.Cookies.Append(AuthenticationCookieNames.RefreshToken, issue.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Path = "/api/auth",
            Expires = expiresAt
        });
        Response.Cookies.Append(AuthenticationCookieNames.CsrfToken, issue.CsrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            IsEssential = true,
            Path = "/api/auth",
            Expires = expiresAt
        });
    }

    private void ClearSessionCookies()
    {
        var options = new CookieOptions { Secure = true, SameSite = SameSiteMode.None, Path = "/api/auth" };
        Response.Cookies.Delete(AuthenticationCookieNames.RefreshToken, options);
        Response.Cookies.Delete(AuthenticationCookieNames.CsrfToken, options);
    }
}

public sealed record RegisterRequest(
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string Surname,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);
public sealed record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public sealed record EmailRequest([Required, EmailAddress] string Email);
public sealed record VerifyEmailCodeRequest([Required, EmailAddress] string Email, [Required, RegularExpression("^[0-9]{6}$")] string Code);
public sealed record ResetPasswordRequest([Required] string UserId, [Required] string Token, [Required, MinLength(8)] string Password);
public sealed record MessageResponse(string Message, string? Code = null);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, string Email, IReadOnlyList<string> Roles);
