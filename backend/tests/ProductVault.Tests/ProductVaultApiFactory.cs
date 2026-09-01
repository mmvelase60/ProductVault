using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductVault;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.Services;

namespace ProductVault.Tests;

public sealed class ProductVaultApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"productvault-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=productvault-tests;",
            ["Jwt:Key"] = "testing-key-that-is-long-enough-for-jwt-signing-123456",
            ["Jwt:Issuer"] = "ProductVault.Api",
            ["Jwt:Audience"] = "ProductVault.Angular"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.RemoveAll<IApplicationEmailSender>();
            services.AddScoped<IApplicationEmailSender, TestEmailSender>();
        });
    }

    public async Task<ApplicationUser> CreateUserAsync(string email, params string[] roles)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FirstName = "Test", Surname = "User" };
        var result = await users.CreateAsync(user, "Password1");
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
        foreach (var role in roles)
        {
            var addRole = await users.AddToRoleAsync(user, role);
            Assert.True(addRole.Succeeded, string.Join(", ", addRole.Errors.Select(error => error.Description)));
        }
        return user;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, params string[] roles)
    {
        await CreateUserAsync(email, roles);
        return await LoginAsync(email);
    }

    public async Task<HttpClient> LoginAsync(string email)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<TestAuthResponse>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session!.AccessToken);
        return client;
    }

    public async Task SeedProductAsync(string ownerId, string name, int productId, int categoryId, byte[] rowVersion)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Categories.Add(new Category { CategoryId = categoryId, Name = $"Category {categoryId}", CategoryCode = $"CAT{categoryId:000}", IsActive = true, OwnerId = ownerId, CreatedBy = ownerId, CreatedDate = DateTime.UtcNow });
        db.Products.Add(new Product { ProductId = productId, ProductCode = $"202609-{productId:000}", Name = name, Price = 100, QuantityInStock = 3, ReorderLevel = 5, CategoryId = categoryId, OwnerId = ownerId, CreatedBy = ownerId, CreatedDate = DateTime.UtcNow, RowVersion = rowVersion });
        await db.SaveChangesAsync();
    }

    private sealed class TestEmailSender : IApplicationEmailSender
    {
        public Task SendAsync(string recipient, string subject, string htmlBody, string plainTextBody, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record TestAuthResponse(string AccessToken);
}
