using System.ComponentModel.DataAnnotations;

namespace ProductVault.Models;

/// <summary>
/// Stores only hashes of long-lived browser session credentials. The raw values
/// are never written to the database and are delivered through secure cookies.
/// </summary>
public sealed class RefreshToken
{
    public int RefreshTokenId { get; set; }

    [Required, MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string CsrfTokenHash { get; set; } = string.Empty;

    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    [MaxLength(128)]
    public string? ReplacedByTokenHash { get; set; }
}
