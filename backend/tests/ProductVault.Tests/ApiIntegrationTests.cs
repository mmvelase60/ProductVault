using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ProductVault.Controllers.Api;
using ProductVault.Models;

namespace ProductVault.Tests;

public sealed class ApiIntegrationTests : IClassFixture<ProductVaultApiFactory>
{
    private readonly ProductVaultApiFactory factory;

    public ApiIntegrationTests(ProductVaultApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Registration_creates_an_unverified_user_with_the_user_role()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var email = $"registered-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new { firstName = "Mthokozisi", surname = "Mvelase", email, password = "Password1" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.False(user!.EmailConfirmed);
        Assert.Contains("User", await users.GetRolesAsync(user));
    }

    [Fact]
    public async Task Verification_code_confirms_an_unverified_account()
    {
        var email = $"verify-{Guid.NewGuid():N}@example.com";
        var created = await factory.CreateUnconfirmedUserWithVerificationCodeAsync(email);
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var response = await client.PostAsJsonAsync("/api/auth/verify-email-code", new { email, code = created.Code });

        response.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        Assert.True((await users.FindByIdAsync(created.User.Id))!.EmailConfirmed);
    }

    [Fact]
    public async Task Duplicate_legacy_email_returns_a_clear_conflict_instead_of_a_server_error()
    {
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        await factory.CreateUserAsync(email, "User");
        await factory.CreateLegacyDuplicateEmailAsync(email);
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var response = await client.PostAsJsonAsync("/api/auth/resend-confirmation", new { email });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.Contains("More than one account", message!.Message);
    }

    [Fact]
    public async Task Refresh_token_rotates_and_logout_revokes_the_browser_session()
    {
        var email = $"session-{Guid.NewGuid():N}@example.com";
        await factory.CreateUserAsync(email, "User");
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password1" });
        login.EnsureSuccessStatusCode();
        var csrf = CookieValue(login, "productvault_csrf");
        Assert.False(string.IsNullOrWhiteSpace(csrf));
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { });
        refresh.EnsureSuccessStatusCode();
        var rotatedCsrf = CookieValue(refresh, "productvault_csrf");
        Assert.NotEqual(csrf, rotatedCsrf);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", rotatedCsrf);

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var afterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Product_list_is_scoped_to_the_authenticated_owner()
    {
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var otherEmail = $"other-{Guid.NewGuid():N}@example.com";
        var owner = await factory.CreateUserAsync(ownerEmail, "User");
        var other = await factory.CreateUserAsync(otherEmail, "User");
        await factory.SeedProductAsync(owner.Id, "Owner keyboard", 101, 101, [1]);
        await factory.SeedProductAsync(other.Id, "Other mouse", 102, 102, [2]);
        var client = await factory.LoginAsync(ownerEmail);

        var response = await client.GetFromJsonAsync<ProductPage>("/api/products");

        Assert.NotNull(response);
        var product = Assert.Single(response!.Items);
        Assert.Equal("Owner keyboard", product.Name);
    }

    [Fact]
    public async Task Stock_movement_records_the_before_and_after_quantities()
    {
        var email = $"stock-{Guid.NewGuid():N}@example.com";
        var user = await factory.CreateUserAsync(email, "User");
        await factory.SeedProductAsync(user.Id, "Stock keyboard", 201, 201, [3]);
        var client = await factory.LoginAsync(email);

        var update = await client.PostAsJsonAsync("/api/products/201/stock-movements", new { operation = "receive", quantity = 4, note = "PO-1001", rowVersion = Convert.ToBase64String(new byte[] { 3 }) });

        update.EnsureSuccessStatusCode();
        var result = await update.Content.ReadFromJsonAsync<StockUpdate>();
        Assert.NotNull(result);
        Assert.Equal(7, result!.Product.QuantityInStock);
        Assert.Equal("Received", result.Movement.Operation);
        var history = await client.GetFromJsonAsync<List<StockMovement>>("/api/products/201/stock-movements");
        var movement = Assert.Single(history!);
        Assert.Equal(3, movement.QuantityBefore);
        Assert.Equal(7, movement.QuantityAfter);
    }

    [Fact]
    public async Task Admin_user_directory_rejects_a_regular_user()
    {
        var client = await factory.CreateAuthenticatedClientAsync($"regular-{Guid.NewGuid():N}@example.com", "User");

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record ProductPage(IReadOnlyList<ProductItem> Items);
    private sealed record ProductItem(string Name);
    private sealed record StockUpdate(StockProduct Product, StockMovement Movement);
    private sealed record StockProduct(int QuantityInStock);
    private sealed record StockMovement(int QuantityBefore, int QuantityAfter, string Operation);

    private static string CookieValue(HttpResponseMessage response, string name)
    {
        var cookie = response.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(value => value.StartsWith($"{name}=", StringComparison.Ordinal));
        Assert.NotNull(cookie);
        return cookie!.Split(';', 2)[0].Substring(name.Length + 1);
    }
}
