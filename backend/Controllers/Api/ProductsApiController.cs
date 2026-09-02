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

[ApiController, Authorize, Route("api/products")]
public class ProductsApiController(ApplicationDbContext db, UserManager<ApplicationUser> users, IProductCodeGenerator codes, IExcelProductService excel, IAuditTrailService audit, IWebHostEnvironment environment) : ControllerBase
{
    private string UserId => users.GetUserId(User)!;

    [HttpGet]
    public async Task<ProductPageResponse> Get(int page = 1, int pageSize = 10, string? search = null, int? categoryId = null, bool lowStock = false, string sort = "newest")
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.OwnerId == UserId);
        search = search?.Trim();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(product => product.Name.Contains(search) || product.ProductCode.Contains(search) || (product.Description != null && product.Description.Contains(search)));
        if (categoryId.HasValue) query = query.Where(product => product.CategoryId == categoryId.Value);
        if (lowStock) query = query.Where(product => product.ReorderLevel > 0 && product.QuantityInStock <= product.ReorderLevel);
        query = sort switch
        {
            "name" => query.OrderBy(product => product.Name),
            "price-asc" => query.OrderBy(product => product.Price),
            "price-desc" => query.OrderByDescending(product => product.Price),
            "code" => query.OrderBy(product => product.ProductCode),
            _ => query.OrderByDescending(product => product.CreatedDate)
        };
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(p => new ProductResponse(p.ProductId, p.ProductCode, p.Name, p.Description, p.Price, p.QuantityInStock, p.ReorderLevel, p.CategoryId, p.Category!.Name, p.ImagePath, Convert.ToBase64String(p.RowVersion))).ToListAsync();
        return new ProductPageResponse(items, page, pageSize, total);
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        if (!IsValid(request)) return BadRequest(new { errors = new { product = "Name, a positive price, and non-negative stock values are required." } });
        var category = await db.Categories.SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.OwnerId == UserId && c.IsActive); if (category is null) return BadRequest(new { errors = new { categoryId = "Choose an active category you own." } });
        Product? product = null;
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            product = new Product { Name = request.Name.Trim(), Description = request.Description?.Trim(), Price = request.Price, QuantityInStock = request.QuantityInStock, ReorderLevel = request.ReorderLevel, CategoryId = category.CategoryId, ProductCode = await codes.NextAsync(DateTime.UtcNow), OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            if (product.QuantityInStock > 0)
                db.InventoryMovements.Add(new InventoryMovement { OwnerId = UserId, ProductId = product.ProductId, ProductName = product.Name, Operation = "Initialised", QuantityBefore = 0, QuantityAfter = product.QuantityInStock, Note = "Initial stock at product creation.", PerformedBy = UserId, OccurredAt = DateTime.UtcNow });
            audit.Record(UserId, UserId, "Created", "Product", product.ProductId.ToString(), product.Name);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });
        ProductVaultMetrics.ProductsCreated.Inc();
        return CreatedAtAction(nameof(Get), ToResponse(product!, category.Name));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        if (!IsValid(request)) return BadRequest(new { errors = new { product = "Name, a positive price, and non-negative stock values are required." } });
        var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId); if (product is null) return NotFound();
        var category = await db.Categories.SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.OwnerId == UserId && c.IsActive); if (category is null) return BadRequest(new { errors = new { categoryId = "Choose an active category you own." } });
        if (string.IsNullOrWhiteSpace(request.RowVersion)) return BadRequest(new { message = "The product version is required." });
        try
        {
            var previousQuantity = product.QuantityInStock;
            product.Name = request.Name.Trim(); product.Description = request.Description?.Trim(); product.Price = request.Price; product.QuantityInStock = request.QuantityInStock; product.ReorderLevel = request.ReorderLevel; product.CategoryId = category.CategoryId; product.UpdatedBy = UserId; product.UpdatedDate = DateTime.UtcNow;
            db.Entry(product).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            if (previousQuantity != product.QuantityInStock)
                db.InventoryMovements.Add(new InventoryMovement { OwnerId = UserId, ProductId = product.ProductId, ProductName = product.Name, Operation = "Adjusted", QuantityBefore = previousQuantity, QuantityAfter = product.QuantityInStock, Note = "Stock changed while editing the product.", PerformedBy = UserId, OccurredAt = DateTime.UtcNow });
            audit.Record(UserId, UserId, "Updated", "Product", product.ProductId.ToString(), product.Name);
            await db.SaveChangesAsync();
            return NoContent();
        }
        catch (FormatException) { return BadRequest(new { message = "The product version is invalid." }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "This product changed in another session. Refresh and try again." }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    { var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId); if (product is null) return NotFound(); var image = product.ImagePath; audit.Record(UserId, UserId, "Deleted", "Product", product.ProductId.ToString(), product.Name); db.Products.Remove(product); await db.SaveChangesAsync(); DeleteImage(image); ProductVaultMetrics.ProductsDeleted.Inc(); return NoContent(); }

    [HttpGet("{id:int}/stock-movements")]
    public async Task<ActionResult<IReadOnlyList<StockMovementResponse>>> StockMovements(int id)
    {
        if (!await db.Products.AnyAsync(product => product.ProductId == id && product.OwnerId == UserId)) return NotFound();
        var movements = await db.InventoryMovements.AsNoTracking().Where(item => item.ProductId == id && item.OwnerId == UserId)
            .OrderByDescending(item => item.OccurredAt).Take(15)
            .Select(item => new StockMovementResponse(item.InventoryMovementId, item.Operation, item.QuantityBefore, item.QuantityAfter, item.Note, item.OccurredAt))
            .ToListAsync();
        return Ok(movements);
    }

    [HttpPost("{id:int}/stock-movements")]
    public async Task<ActionResult<StockUpdateResponse>> AdjustStock(int id, StockAdjustmentRequest request)
    {
        var operation = request.Operation.Trim().ToLowerInvariant();
        if (operation is not "receive" and not "set") return BadRequest(new { message = "Operation must be receive or set." });
        if ((operation == "receive" && request.Quantity <= 0) || (operation == "set" && request.Quantity < 0)) return BadRequest(new { message = "Receive requires a positive quantity; set requires a non-negative quantity." });
        if (string.IsNullOrWhiteSpace(request.RowVersion)) return BadRequest(new { message = "The product version is required." });

        var product = await db.Products.Include(item => item.Category).SingleOrDefaultAsync(item => item.ProductId == id && item.OwnerId == UserId);
        if (product is null) return NotFound();
        try
        {
            var previousQuantity = product.QuantityInStock;
            var nextQuantity = operation == "receive" ? checked(previousQuantity + request.Quantity) : request.Quantity;
            product.QuantityInStock = nextQuantity;
            product.UpdatedBy = UserId;
            product.UpdatedDate = DateTime.UtcNow;
            db.Entry(product).Property(item => item.RowVersion).OriginalValue = Convert.FromBase64String(request.RowVersion);
            var movement = new InventoryMovement { OwnerId = UserId, ProductId = product.ProductId, ProductName = product.Name, Operation = operation == "receive" ? "Received" : "Adjusted", QuantityBefore = previousQuantity, QuantityAfter = nextQuantity, Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(), PerformedBy = UserId, OccurredAt = DateTime.UtcNow };
            db.InventoryMovements.Add(movement);
            audit.Record(UserId, UserId, movement.Operation, "Stock", product.ProductId.ToString(), product.Name, $"{previousQuantity} → {nextQuantity}");
            await db.SaveChangesAsync();
            return Ok(new StockUpdateResponse(ToResponse(product, product.Category!.Name), new StockMovementResponse(movement.InventoryMovementId, movement.Operation, movement.QuantityBefore, movement.QuantityAfter, movement.Note, movement.OccurredAt)));
        }
        catch (OverflowException) { return BadRequest(new { message = "The resulting quantity is too large." }); }
        catch (FormatException) { return BadRequest(new { message = "The product version is invalid." }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "This product changed in another session. Refresh and try again." }); }
    }

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
            audit.Record(UserId, UserId, "Updated image", "Product", product.ProductId.ToString(), product.Name);
            await db.SaveChangesAsync();
            DeleteImage(previousImage);
            return Ok(ToResponse(product, product.Category!.Name));
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
            await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                foreach (var row in rows)
                {
                    db.Products.Add(new Product { Name = row.Name, Description = row.Description, Price = row.Price, CategoryId = categories[row.CategoryCode].CategoryId, ProductCode = await codes.NextAsync(DateTime.UtcNow), OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow });
                    await db.SaveChangesAsync();
                }
                await transaction.CommitAsync();
            });
            ProductVaultMetrics.ProductsCreated.Inc(rows.Count);
            audit.Record(UserId, UserId, "Imported", "Product catalogue", "excel", "Excel product import", $"Imported {rows.Count} products.");
            await db.SaveChangesAsync();
            return Ok(new { imported = rows.Count });
        }
        catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        catch { return BadRequest(new { message = "The Excel file could not be imported. Check that it is a valid .xlsx workbook." }); }
    }

    private async Task<string?> SaveImageAsync(IFormFile? image)
    {
        if (image is null || image.Length == 0) return null;
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (image.Length > 5 * 1024 * 1024 || !new[] { ".jpg", ".jpeg", ".jfif", ".png", ".gif", ".webp" }.Contains(extension))
            throw new InvalidOperationException("Upload a JPG, JFIF, PNG, GIF, or WEBP image smaller than 5 MB.");

        // JFIF is a JPEG file format. Saving it with the standard extension ensures
        // browsers and the static-file middleware consistently serve it as an image.
        if (extension == ".jfif") extension = ".jpg";
        var folder = Path.Combine(environment.WebRootPath, "uploads", "product-images");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
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

    private static bool IsValid(ProductRequest request) => !string.IsNullOrWhiteSpace(request.Name) && request.Price > 0 && request.QuantityInStock >= 0 && request.ReorderLevel >= 0;

    private static ProductResponse ToResponse(Product product, string categoryName) => new(product.ProductId, product.ProductCode, product.Name, product.Description, product.Price, product.QuantityInStock, product.ReorderLevel, product.CategoryId, categoryName, product.ImagePath, Convert.ToBase64String(product.RowVersion));
}

public sealed record ProductRequest(string Name, string? Description, decimal Price, int QuantityInStock, int ReorderLevel, int CategoryId, string? RowVersion = null);
public sealed record ProductResponse(int ProductId, string ProductCode, string Name, string? Description, decimal Price, int QuantityInStock, int ReorderLevel, int CategoryId, string CategoryName, string? ImagePath, string RowVersion)
{
    public bool IsLowStock => ReorderLevel > 0 && QuantityInStock <= ReorderLevel;
}
public sealed record ProductPageResponse(IReadOnlyList<ProductResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record StockAdjustmentRequest(string Operation, int Quantity, string? Note, string? RowVersion);
public sealed record StockMovementResponse(long InventoryMovementId, string Operation, int QuantityBefore, int QuantityAfter, string? Note, DateTime OccurredAt);
public sealed record StockUpdateResponse(ProductResponse Product, StockMovementResponse Movement);
