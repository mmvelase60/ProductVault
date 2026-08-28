using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;

namespace ProductVault.Tests;

public class ProductCodeGeneratorTests
{
    [Fact]
    public async Task NextAsync_starts_a_month_at_001()
    {
        await using var db = CreateContext();
        var generator = new ProductCodeGenerator(db);

        var code = await generator.NextAsync(new DateTime(2026, 8, 1));

        Assert.Equal("202608-001", code);
    }

    [Fact]
    public async Task NextAsync_continues_the_current_month_sequence()
    {
        await using var db = CreateContext();
        db.Products.Add(new Product { ProductCode = "202608-009", Name = "Mouse", Price = 99, CategoryId = 1, OwnerId = "user-1", CreatedBy = "user-1", CreatedDate = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var generator = new ProductCodeGenerator(db);

        var code = await generator.NextAsync(new DateTime(2026, 8, 28));

        Assert.Equal("202608-010", code);
    }

    [Fact]
    public async Task NextAsync_resets_for_a_new_month()
    {
        await using var db = CreateContext();
        db.Products.Add(new Product { ProductCode = "202608-999", Name = "Mouse", Price = 99, CategoryId = 1, OwnerId = "user-1", CreatedBy = "user-1", CreatedDate = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var generator = new ProductCodeGenerator(db);

        var code = await generator.NextAsync(new DateTime(2026, 9, 1));

        Assert.Equal("202609-001", code);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
