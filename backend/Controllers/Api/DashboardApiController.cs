using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ProductVault.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Monitoring;
using ProductVault.Services;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize, Route("api/dashboard")]
public class DashboardApiController(ApplicationDbContext db, UserManager<ApplicationUser> users, IProductCodeGenerator codes, IAuditTrailService audit) : ControllerBase
{
    private string UserId => users.GetUserId(User)!;

    [HttpGet]
    public async Task<DashboardResponse> Get()
    {
        var products = db.Products.AsNoTracking().Where(product => product.OwnerId == UserId);
        var categories = db.Categories.AsNoTracking().Where(category => category.OwnerId == UserId);
        var recent = await products.Include(product => product.Category).OrderByDescending(product => product.CreatedDate).Take(5)
            .Select(product => new RecentProductResponse(product.ProductId, product.Name, product.ProductCode, product.Price, product.Category!.Name, product.ImagePath)).ToListAsync();
        var activity = await db.AuditEvents.AsNoTracking().Where(item => item.OwnerId == UserId).OrderByDescending(item => item.OccurredAt).Take(6)
            .Select(item => new AuditEventResponse(item.Action, item.EntityType, item.EntityName, item.Detail, item.OccurredAt)).ToListAsync();
        return new DashboardResponse(
            await products.CountAsync(),
            await categories.CountAsync(category => category.IsActive),
            await categories.CountAsync(),
            await products.SumAsync(product => (decimal?)product.Price) ?? 0,
            await products.CountAsync(product => product.ReorderLevel > 0 && product.QuantityInStock <= product.ReorderLevel),
            recent,
            activity);
    }

    [HttpPost("demo-data")]
    public async Task<IActionResult> SeedDemoData()
    {
        if (await db.Categories.AnyAsync(category => category.OwnerId == UserId) || await db.Products.AnyAsync(product => product.OwnerId == UserId))
            return Conflict(new { message = "Demo data can only be loaded into an empty workspace." });

        var now = DateTime.UtcNow;
        var categories = new[]
        {
            new Category { Name = "Office supplies", CategoryCode = "OFF101", IsActive = true, OwnerId = UserId, CreatedBy = UserId, CreatedDate = now },
            new Category { Name = "Technology", CategoryCode = "TEC202", IsActive = true, OwnerId = UserId, CreatedBy = UserId, CreatedDate = now },
            new Category { Name = "Home office", CategoryCode = "HOM303", IsActive = true, OwnerId = UserId, CreatedBy = UserId, CreatedDate = now }
        };

        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();

            var products = new[]
            {
                new Product { Name = "Wireless keyboard", Description = "Compact Bluetooth keyboard for everyday work.", Price = 899.99m, QuantityInStock = 7, ReorderLevel = 10, CategoryId = categories[1].CategoryId, OwnerId = UserId, CreatedBy = UserId, CreatedDate = now },
                new Product { Name = "USB-C hub", Description = "Seven-port hub with HDMI and Ethernet.", Price = 649.50m, QuantityInStock = 25, ReorderLevel = 5, CategoryId = categories[1].CategoryId, OwnerId = UserId, CreatedBy = UserId, CreatedDate = now },
                new Product { Name = "Ergonomic mouse", Description = "Comfortable wireless mouse with adjustable DPI.", Price = 459.00m, QuantityInStock = 4, ReorderLevel = 8, CategoryId = categories[1].CategoryId, OwnerId = UserId, CreatedBy = UserId, CreatedDate = now },
                new Product { Name = "A4 printer paper", Description = "500-sheet ream of bright white copy paper.", Price = 129.99m, QuantityInStock = 120, ReorderLevel = 30, CategoryId = categories[0].CategoryId, OwnerId = UserId, CreatedBy = UserId, CreatedDate = now },
                new Product { Name = "Desk organiser", Description = "Bamboo organiser for a tidy home-office desk.", Price = 249.00m, QuantityInStock = 14, ReorderLevel = 4, CategoryId = categories[2].CategoryId, OwnerId = UserId, CreatedBy = UserId, CreatedDate = now }
            };

            foreach (var product in products)
            {
                product.ProductCode = await codes.NextAsync(now);
                db.Products.Add(product);
                await db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        });
        ProductVaultMetrics.CategoriesCreated.Inc(categories.Length);
        ProductVaultMetrics.ProductsCreated.Inc(5);
        audit.Record(UserId, UserId, "Loaded", "Catalogue", "demo-data", "Starter catalogue", "Loaded 3 categories and 5 products.");
        await db.SaveChangesAsync();
        return Ok(new { message = "Demo data loaded.", categories = categories.Length, products = 5 });
    }
}

public sealed record DashboardResponse(int ProductCount, int ActiveCategoryCount, int TotalCategoryCount, decimal CatalogueValue, int LowStockCount, IReadOnlyList<RecentProductResponse> RecentProducts, IReadOnlyList<AuditEventResponse> Activity);
public sealed record RecentProductResponse(int ProductId, string Name, string ProductCode, decimal Price, string CategoryName, string? ImagePath);
public sealed record AuditEventResponse(string Action, string EntityType, string EntityName, string? Detail, DateTime OccurredAt);
