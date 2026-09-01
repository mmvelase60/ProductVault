using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using ProductVault.Models;

namespace ProductVault.Services;

public sealed class UsernameGenerator(UserManager<ApplicationUser> users) : IUsernameGenerator
{
    public async Task<string> NextAsync(string firstName, string surname, string? excludedUserId = null, CancellationToken cancellationToken = default)
    {
        var initial = char.ToUpperInvariant(firstName.First(char.IsLetterOrDigit));
        var cleanedSurname = Regex.Replace(surname, @"[^\p{L}\p{Nd}]", string.Empty);
        var baseUsername = $"{initial}{cleanedSurname}";

        for (var suffix = 1; ; suffix++)
        {
            var candidate = suffix == 1 ? baseUsername : $"{baseUsername}{suffix}";
            var existing = await users.FindByNameAsync(candidate);
            if (existing is null || existing.Id == excludedUserId)
                return candidate;
        }
    }
}
