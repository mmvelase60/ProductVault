using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductVault.Data;
using ProductVault.Models;

namespace ProductVault.Services;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "Authentication";
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
    public int RefreshTokenLifetimeDays { get; init; } = 7;
}

public static class AuthenticationCookieNames
{
    public const string RefreshToken = "productvault_refresh";
    public const string CsrfToken = "productvault_csrf";
    public const string CsrfHeader = "X-CSRF-TOKEN";
}

public sealed record RefreshTokenIssue(string Token, string CsrfToken, DateTime ExpiresAt);
public sealed record RefreshTokenRefreshResult(ApplicationUser User, RefreshTokenIssue Issue);

public interface IRefreshTokenService
{
    Task<RefreshTokenIssue> IssueAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<RefreshTokenRefreshResult?> RotateAsync(string token, string csrfToken, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string token, string csrfToken, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class RefreshTokenService(
    ApplicationDbContext db,
    IOptions<RefreshTokenOptions> options) : IRefreshTokenService
{
    private readonly RefreshTokenOptions options = options.Value;

    public async Task<RefreshTokenIssue> IssueAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var issue = CreateIssue();
        db.RefreshTokens.Add(ToEntity(user.Id, issue));
        await db.SaveChangesAsync(cancellationToken);
        return issue;
    }

    public async Task<RefreshTokenRefreshResult?> RotateAsync(string token, string csrfToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(token);
        var storedToken = await db.RefreshTokens.Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !MatchesHash(csrfToken, storedToken.CsrfTokenHash))
            return null;

        if (storedToken.RevokedAt is not null)
        {
            await RevokeAllAsync(storedToken.UserId, cancellationToken);
            return null;
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
            return null;

        var issue = CreateIssue();
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = Hash(issue.Token);
        db.RefreshTokens.Add(ToEntity(storedToken.UserId, issue));
        await db.SaveChangesAsync(cancellationToken);
        return new RefreshTokenRefreshResult(storedToken.User, issue);
    }

    public async Task<bool> RevokeAsync(string token, string csrfToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await db.RefreshTokens.SingleOrDefaultAsync(item => item.TokenHash == Hash(token), cancellationToken);
        if (storedToken is null || !MatchesHash(csrfToken, storedToken.CsrfTokenHash))
            return false;

        if (storedToken.RevokedAt is null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task RevokeAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tokens = await db.RefreshTokens
            .Where(item => item.UserId == userId && item.RevokedAt == null)
            .ToListAsync(cancellationToken);
        if (tokens.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var token in tokens) token.RevokedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private RefreshTokenIssue CreateIssue()
    {
        var expiresAt = DateTime.UtcNow.AddDays(Math.Clamp(options.RefreshTokenLifetimeDays, 1, 30));
        return new RefreshTokenIssue(CreateSecret(), CreateSecret(), expiresAt);
    }

    private static RefreshToken ToEntity(string userId, RefreshTokenIssue issue) => new()
    {
        UserId = userId,
        TokenHash = Hash(issue.Token),
        CsrfTokenHash = Hash(issue.CsrfToken),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = issue.ExpiresAt
    };

    private static string CreateSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
    private static string Hash(string value) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool MatchesHash(string value, string expectedHash)
    {
        var suppliedHash = Hash(value);
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(suppliedHash), Encoding.UTF8.GetBytes(expectedHash));
    }
}
