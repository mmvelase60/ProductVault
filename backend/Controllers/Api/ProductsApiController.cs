using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.Monitoring;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize, Route("api/products")]
public class ProductsApiController(ApplicationDbContext db, UserManager<IdentityUser> users, IProductCodeGenerator codes, IExcelProductService excel, IWebHostEnvironment environment) : ControllerBase
{
    private string UserId => users.GetUserId(User)!;

    [HttpGet]
    public async Task<ProductPageResponse> Get(int page = 1, int pageSize = 10, string? search = null, int? categoryId = null, string sort = "newest")
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.OwnerId == UserId);
        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(product => product.Name.Contains(search) || product.ProductCode.Contains(search) || (product.Description != null && product.Description.Contains(search)));
        if (categoryId.HasValue) query = query.Where(product => product.CategoryId == categoryId.Value);
        query = sort switch
        {
            "name" => query.OrderBy(product => product.Name),
            "price-asc" => query.OrderBy(product => product.Price),
            "price-desc" => query.OrderByDescending(product => product.Price),
            "code" => query.OrderBy(product => product.ProductCode),
            _ => query.OrderByDescending(product => product.CreatedDate)
        };
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(p => new ProductResponse(p.ProductId, p.ProductCode, p.Name, p.Description, p.Price, p.CategoryId, p.Category!.Name, p.ImagePath, Convert.ToBase64String(p.RowVersion))).ToListAsync();
        return new ProductPageResponse(items, page, pageSize, total);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) return BadRequest(new { errors = new { product = "Name is required and price must be greater than zero." } });
        var category = await db.Categories.SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.OwnerId == UserId && c.IsActive); if (category is null) return BadRequest(new { errors = new { categoryId = "Choose an active category you own." } });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var product = new Product { Name = request.Name.Trim(), Description = request.Description?.Trim(), Price = request.Price, CategoryId = category.CategoryId, ProductCode = await codes.NextAsync(DateTime.UtcNow), OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow };
        db.Products.Add(product); await db.SaveChangesAsync(); await transaction.CommitAsync();
        ProductVaultMetrics.ProductsCreated.Inc();
        return CreatedAtAction(nameof(Get), new ProductResponse(product.ProductId, product.ProductCode, product.Name, product.Description, product.Price, category.CategoryId, category.Name, product.ImagePath, Convert.ToBase64String(product.RowVersion)));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) return BadRequest(new { errors = new { product = "Name is required and price must be greater than zero." } });
        var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId); if (product is null) return NotFound();
        var category = await db.Categories.SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.OwnerId == UserId && c.IsActive); if (category is null) return BadRequest(new { errors = new { categoryId = "Choose an active category you own." } });
        if (string.IsNullOrWhiteSpace(request.RowVersion)) return BadRequest(new { message = "The product version is required." });
        try
        {
            product.Name = request.Name.Trim(); product.Description = request.Description?.Trim(); product.Price = request.Price; product.CategoryId = category.CategoryId; product.UpdatedBy = UserId; product.UpdatedDate = DateTime.UtcNow;
            db.Entry(product).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            await db.SaveChangesAsync();
            return NoContent();
        }
        catch (FormatException) { return BadRequest(new { message = "The product version is invalid." }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "This product changed in another session. Refresh and try again." }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    { var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId); if (product is null) return NotFound(); var image = product.ImagePath; db.Products.Remove(product); await db.SaveChangesAsync(); DeleteImage(image); ProductVaultMetrics.ProductsDeleted.Inc(); return NoContent(); }

    [HttpPost("{id:int}/image")]
    public async Task<ActionResult<ProductResponse>> UploadImage(int id, IFormFile? file)
    {
        var product = await db.Products.Include(item => item.Category).SingleOrDefaultAsync(item => item.ProductId == id && item.OwnerId == UserId);
        if (product is null) return NotFound();
        try
        {
            var image = await SaveImageAsync(file);
            if (image is null) return BadRequest(new { message = "Choose an image to upload." });
            var previousImage = product.ImagePath;
            product.ImagePath = image;
            product.UpdatedBy = UserId;
            product.UpdatedDate = DateTime.UtcNow;
            await db.SaveChangesAsync();
            DeleteImage(previousImage);
            return Ok(new ProductResponse(product.ProductId, product.ProductCode, product.Name, product.Description, product.Price, product.CategoryId, product.Category!.Name, product.ImagePath, Convert.ToBase64String(product.RowVersion)));
        }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var products = await db.Products.AsNoTracking().Include(product => product.Category).Where(product => product.OwnerId == UserId).OrderBy(product => product.Name).ToListAsync();
        return File(excel.Export(products), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"products-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Choose an Excel file to import." });
        if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".xlsx") return BadRequest(new { message = "Only .xlsx files are supported." });
        try
        {
            var rows = excel.Read(file);
            if (rows.Count == 0 || rows.Count > 500) return BadRequest(new { message = "The file must contain between 1 and 500 product rows." });
            var categories = await db.Categories.Where(category => category.OwnerId == UserId && category.IsActive).ToDictionaryAsync(category => category.CategoryCode, StringComparer.OrdinalIgnoreCase);
            var invalid = rows.Select((row, index) => new { row, index }).FirstOrDefault(item => item.row.Price <= 0 || string.IsNullOrWhiteSpace(item.row.Name) || !categories.ContainsKey(item.row.CategoryCode));
            if (invalid is not null) return BadRequest(new { message = $"Row {invalid.index + 2} has an invalid name, price, or active category code." });
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            foreach (var row in rows)
            {
                db.Products.Add(new Product { Name = row.Name, Description = row.Description, Price = row.Price, CategoryId = categories[row.CategoryCode].CategoryId, ProductCode = await codes.NextAsync(DateTime.UtcNow), OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            ProductVaultMetrics.ProductsCreated.Inc(rows.Count);
            return Ok(new { imported = rows.Count });
        }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        catch { return BadRequest(new { message = "The Excel file could not be imported. Check that it is a valid .xlsx workbook." }); }
    }

    private async Task<string?> SaveImageAsync(IFormFile? image)
    {
        if (image is null || image.Length == 0) return null;
        if (image.Length > 5 * 1024 * 1024 || !new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(Path.GetExtension(image.FileName).ToLowerInvariant())) throw new InvalidOperationException("Upload a JPG, PNG, GIF, or WEBP image smaller than 5 MB.");
        var folder = Path.Combine(environment.WebRootPath, "uploads", "product-images");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);
        await using var stream = System.IO.File.Create(fullPath);
        await image.CopyToAsync(stream);
        return $"/uploads/product-images/{fileName}";
    }

    private void DeleteImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var file = Path.Combine(environment.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
    }
}

public sealed record ProductRequest(string Name, string? Description, decimal Price, int CategoryId, string? RowVersion = null);
public sealed record ProductResponse(int ProductId, string ProductCode, string Name, string? Description, decimal Price, int CategoryId, string CategoryName, string? ImagePath, string RowVersion);
public sealed record ProductPageResponse(IReadOnlyList<ProductResponse> Items, int Page, int PageSize, int TotalCount);
