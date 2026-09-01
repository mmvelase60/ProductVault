using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.Services;

namespace ProductVault.Tests;

public class InventoryAndAuditTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(12, 4, true)]
    [InlineData(-1, 0, false)]
    [InlineData(5, -1, false)]
    public void Product_inventory_values_must_be_non_negative(int quantity, int reorderLevel, bool expectedValid)
    {
        var product = new Product
        {
            ProductCode = "202609-001", Name = "Keyboard", Price = 899.99m,
            QuantityInStock = quantity, ReorderLevel = reorderLevel, CategoryId = 1,
            OwnerId = "user-1", CreatedBy = "user-1", CreatedDate = DateTime.UtcNow
        };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(product, new ValidationContext(product), results, validateAllProperties: true);

        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public async Task Audit_service_records_an_owner_scoped_event()
    {
        await using var db = CreateContext();
        var audit = new AuditTrailService(db);

        audit.Record("owner-1", "actor-1", "Updated", "Product", "42", "Wireless keyboard", "Stock changed from 8 to 6.");
        await db.SaveChangesAsync();

        var entry = await db.AuditEvents.SingleAsync();
        Assert.Equal("owner-1", entry.OwnerId);
        Assert.Equal("Updated", entry.Action);
        Assert.Equal("Product", entry.EntityType);
        Assert.Equal("Wireless keyboard", entry.EntityName);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
