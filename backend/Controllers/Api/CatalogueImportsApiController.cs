using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.Monitoring;
using ProductVault.Services;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize, Route("api/catalogue-imports")]
public sealed class CatalogueImportsApiController(ApplicationDbContext db, UserManager<ApplicationUser> users, IExcelProductService excel, IProductCodeGenerator codes, IAuditTrailService audit) : ControllerBase
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
            var errors = await ValidateAsync(rows);
            var validRows = rows.Where(row => errors.All(error => error.RowNumber != row.RowNumber)).ToList();
            var result = validRows.Count == 0 ? new CatalogueImportResult(0, 0, 0, errors) : await ImportAsync(validRows, errors);
            return Ok(result);
        }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpGet("template")]
    public IActionResult Template() => File(System.Text.Encoding.UTF8.GetBytes("Category Name,Category Code,Category Active,Product Name,Description,Price,Quantity In Stock,Reorder Level\nTechnology,TEC202,true,Wireless keyboard,Bluetooth keyboard,899.99,25,5\n"), "text/csv", "productvault-catalogue-template.csv");

    private async Task<List<CatalogueImportError>> ValidateAsync(IReadOnlyList<CatalogueImportRow> rows)
    {
        var errors = new List<CatalogueImportError>();
        foreach (var row in rows)
        {
            var message = string.IsNullOrWhiteSpace(row.CategoryName) ? "Category Name is required."
                : !Regex.IsMatch(row.CategoryCode, "^[A-Z]{3}[0-9]{3}$") ? "Category Code must follow ABC123."
                : !row.HasValidCategoryActive ? "Category Active must be true or false."
                : !string.IsNullOrWhiteSpace(row.ProductName) && (!row.HasValidPrice || row.Price <= 0) ? "Price must be a positive number using a decimal point."
                : !row.HasValidQuantity || row.QuantityInStock < 0 ? "Quantity In Stock must be a non-negative whole number."
                : !row.HasValidReorderLevel || row.ReorderLevel < 0 ? "Reorder Level must be a non-negative whole number."
                : string.IsNullOrWhiteSpace(row.ProductName) ? "Product Name is required."
                : null;
            if (message is not null) errors.Add(new CatalogueImportError(row.RowNumber, row.ProductName, message));
        }

        var inactiveCodes = await db.Categories.Where(category => category.OwnerId == UserId && !category.IsActive).Select(category => category.CategoryCode).ToListAsync();
        foreach (var row in rows.Where(row => !row.CategoryActive || inactiveCodes.Contains(row.CategoryCode, StringComparer.OrdinalIgnoreCase)))
            if (errors.All(error => error.RowNumber != row.RowNumber))
                errors.Add(new CatalogueImportError(row.RowNumber, row.ProductName, "Products can only be imported into an active category."));
        return errors;
    }

    private async Task<CatalogueImportResult> ImportAsync(IReadOnlyList<CatalogueImportRow> rows, IReadOnlyList<CatalogueImportError> errors)
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
                var key = $"{category.CategoryId}|{source.ProductName}".ToUpperInvariant();
                if (!keys.Add(key)) { skipped++; continue; }
                db.Products.Add(new Product { Name = source.ProductName, Description = source.Description, Price = source.Price, QuantityInStock = source.QuantityInStock, ReorderLevel = source.ReorderLevel, CategoryId = category.CategoryId, ProductCode = await codes.NextAsync(DateTime.UtcNow), OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow });
                await db.SaveChangesAsync(); productCount++;
            }
            await transaction.CommitAsync();
        });
        if (categoryCount > 0) ProductVaultMetrics.CategoriesCreated.Inc(categoryCount);
        if (productCount > 0) ProductVaultMetrics.ProductsCreated.Inc(productCount);
        audit.Record(UserId, UserId, "Imported", "Catalogue", "file", "Catalogue file import", $"Created {categoryCount} categories and {productCount} products; skipped {skipped} duplicates; rejected {errors.Count} rows.");
        await db.SaveChangesAsync();
        return new CatalogueImportResult(categoryCount, productCount, skipped, errors);
    }
}

public sealed record CatalogueImportResult(int CategoriesCreated, int ProductsCreated, int ProductsSkipped, IReadOnlyList<CatalogueImportError> Errors);
public sealed record CatalogueImportError(int RowNumber, string ProductName, string Message);
