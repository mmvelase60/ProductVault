using Microsoft.AspNetCore.Identity;

namespace ProductVault.Models;

public sealed class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? Surname { get; set; }
}
