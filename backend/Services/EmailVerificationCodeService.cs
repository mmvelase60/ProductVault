using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ProductVault.Services;

public sealed class EmailVerificationCodeService(
    UserManager<IdentityUser> users,
    IPasswordHasher<IdentityUser> passwordHasher,
    IOptions<EmailOptions> options) : IEmailVerificationCodeService
{
    private const string Provider = "ProductVault.EmailVerification";
    private const string Name = "VerificationCode";
    private const int MaximumAttempts = 5;

    public async Task<string> CreateAsync(IdentityUser user, CancellationToken cancellationToken = default)
    {
        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString(CultureInfo.InvariantCulture);
        var token = new VerificationCodeToken(
            passwordHasher.HashPassword(user, code),
            DateTimeOffset.UtcNow.AddMinutes(options.Value.VerificationCodeLifetimeMinutes),
            0);
        await users.SetAuthenticationTokenAsync(user, Provider, Name, JsonSerializer.Serialize(token));
        return code;
    }

    public async Task<EmailVerificationCodeResult> VerifyAsync(IdentityUser user, string code, CancellationToken cancellationToken = default)
    {
        var value = await users.GetAuthenticationTokenAsync(user, Provider, Name);
        var token = Deserialize(value);
        if (token is null || token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await users.RemoveAuthenticationTokenAsync(user, Provider, Name);
            return EmailVerificationCodeResult.Expired;
        }

        if (token.FailedAttempts >= MaximumAttempts)
            return EmailVerificationCodeResult.TooManyAttempts;

        var verification = passwordHasher.VerifyHashedPassword(user, token.Hash, code);
        if (verification is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded)
        {
            await users.RemoveAuthenticationTokenAsync(user, Provider, Name);
            return EmailVerificationCodeResult.Success;
        }

        var updated = token with { FailedAttempts = token.FailedAttempts + 1 };
        if (updated.FailedAttempts >= MaximumAttempts)
        {
            await users.RemoveAuthenticationTokenAsync(user, Provider, Name);
            return EmailVerificationCodeResult.TooManyAttempts;
        }

        await users.SetAuthenticationTokenAsync(user, Provider, Name, JsonSerializer.Serialize(updated));
        return EmailVerificationCodeResult.Invalid;
    }

    private static VerificationCodeToken? Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return JsonSerializer.Deserialize<VerificationCodeToken>(value); }
        catch (JsonException) { return null; }
    }

    private sealed record VerificationCodeToken(string Hash, DateTimeOffset ExpiresAt, int FailedAttempts);
}
