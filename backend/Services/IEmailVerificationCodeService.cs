using Microsoft.AspNetCore.Identity;

namespace ProductVault.Services;

public interface IEmailVerificationCodeService
{
    Task<string> CreateAsync(IdentityUser user, CancellationToken cancellationToken = default);
    Task<EmailVerificationCodeResult> VerifyAsync(IdentityUser user, string code, CancellationToken cancellationToken = default);
}

public enum EmailVerificationCodeResult
{
    Success,
    Invalid,
    Expired,
    TooManyAttempts
}
