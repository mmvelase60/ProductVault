using Microsoft.AspNetCore.Identity;
using ProductVault.Models;

namespace ProductVault.Services;

public sealed class RoleBootstrapper(RoleManager<IdentityRole> roles, UserManager<ApplicationUser> users, IConfiguration configuration, ILogger<RoleBootstrapper> logger)
{
    public const string UserRole = "User";
    public const string AdminRole = "Admin";

    public async Task InitialiseAsync()
    {
        foreach (var roleName in new[] { UserRole, AdminRole })
            if (!await roles.RoleExistsAsync(roleName))
            {
                var result = await roles.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded) throw new InvalidOperationException($"Could not create the {roleName} role.");
            }

        var configuredAdminEmail = configuration["Admin:Email"]?.Trim();
        if (string.IsNullOrWhiteSpace(configuredAdminEmail)) return;
        var admin = await users.FindByEmailAsync(configuredAdminEmail);
        if (admin is null)
        {
            logger.LogInformation("Admin role bootstrap is waiting for the configured account to register.");
            return;
        }

        if (!await users.IsInRoleAsync(admin, AdminRole))
            await users.AddToRoleAsync(admin, AdminRole);
    }

    public bool IsConfiguredAdmin(string email) => string.Equals(configuration["Admin:Email"]?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase);
}
