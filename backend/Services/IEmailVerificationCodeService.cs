using Microsoft.AspNetCore.Identity;
using ProductVault.Models;

namespace ProductVault.Services;

public interface IEmailVerificationCodeService
{
    Task<string> CreateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<EmailVerificationCodeResult> VerifyAsync(ApplicationUser user, string code, CancellationToken cancellationToken = default);
}

public enum EmailVerificationCodeResult
{
    Success,
    Invalid,
    Expired,
    TooManyAttempts
}
