using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.Monitoring;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize, Route("api/catalogue-imports")]
public sealed class CatalogueImportsApiController(ApplicationDbContext db, UserManager<ApplicationUser> users, IExcelProductService excel, IProductCodeGenerator codes) : ControllerBase
{
    private string UserId => users.GetUserId(User)!;

    [HttpPost("file")]
    public async Task<ActionResult<CatalogueImportResult>> Import(IFormFile? file)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Choose a CSV or Excel catalogue file." });
        try
        {
            var rows = excel.ReadCatalogue(file);
            if (rows.Count is < 1 or > 500) return BadRequest(new { message = "The file must contain between 1 and 500 catalogue rows." });
            Validate(rows);
            var result = await ImportAsync(rows);
            return Ok(result);
        }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpGet("template")]
    public IActionResult Template() => File(System.Text.Encoding.UTF8.GetBytes("Category Name,Category Code,Category Active,Product Name,Description,Price\nTechnology,TEC202,true,Wireless keyboard,Bluetooth keyboard,899.99\n"), "text/csv", "productvault-catalogue-template.csv");

    private static void Validate(IEnumerable<CatalogueImportRow> rows)
    {
        foreach (var row in rows)
            if (string.IsNullOrWhiteSpace(row.CategoryName) || string.IsNullOrWhiteSpace(row.ProductName) || row.Price <= 0 || !Regex.IsMatch(row.CategoryCode, "^[A-Z]{3}[0-9]{3}$"))
                throw new InvalidOperationException("Each row needs a category name, an ABC123 category code, a product name, and a positive price.");
    }

    private async Task<CatalogueImportResult> ImportAsync(IReadOnlyList<CatalogueImportRow> rows)
    {
        var categoryCount = 0; var productCount = 0; var skipped = 0;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var categories = await db.Categories.Where(item => item.OwnerId == UserId).ToDictionaryAsync(item => item.CategoryCode, StringComparer.OrdinalIgnoreCase);
            foreach (var source in rows.GroupBy(item => item.CategoryCode, StringComparer.OrdinalIgnoreCase).Select(group => group.First()))
                if (!categories.ContainsKey(source.CategoryCode)) { var category = new Category { Name = source.CategoryName, CategoryCode = source.CategoryCode, IsActive = source.CategoryActive, OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow }; db.Categories.Add(category); categories.Add(category.CategoryCode, category); categoryCount++; }
            await db.SaveChangesAsync();
            var existing = await db.Products.Where(item => item.OwnerId == UserId).Select(item => new { item.CategoryId, item.Name }).ToListAsync();
            var keys = existing.Select(item => $"{item.CategoryId}|{item.Name}".ToUpperInvariant()).ToHashSet();
            foreach (var source in rows)
            {
                var category = categories[source.CategoryCode];
                if (!category.IsActive) throw new InvalidOperationException($"Category {source.CategoryCode} is inactive.");
                var key = $"{category.CategoryId}|{source.ProductName}".ToUpperInvariant();
                if (!keys.Add(key)) { skipped++; continue; }
                db.Products.Add(new Product { Name = source.ProductName, Description = source.Description, Price = source.Price, CategoryId = category.CategoryId, ProductCode = await codes.NextAsync(DateTime.UtcNow), OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow });
                await db.SaveChangesAsync(); productCount++;
            }
            await transaction.CommitAsync();
        });
        if (categoryCount > 0) ProductVaultMetrics.CategoriesCreated.Inc(categoryCount);
        if (productCount > 0) ProductVaultMetrics.ProductsCreated.Inc(productCount);
        return new CatalogueImportResult(categoryCount, productCount, skipped);
    }
}

public sealed record CatalogueImportResult(int CategoriesCreated, int ProductsCreated, int ProductsSkipped);
