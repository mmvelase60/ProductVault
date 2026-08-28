using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.ViewModels;

namespace ProductVault.Controllers;

[Authorize]
public class ProductsController(ApplicationDbContext db, UserManager<IdentityUser> userManager, IProductCodeGenerator codes, IExcelProductService excel, IWebHostEnvironment environment) : Controller
{
    private const int PageSize = 10;
    private string UserId => userManager.GetUserId(User)!;

    public async Task<IActionResult> Index(int page = 1)
    {
        page = Math.Max(page, 1);
        var query = db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.OwnerId == UserId).OrderByDescending(p => p.CreatedDate);
        var count = await query.CountAsync(); var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize)); page = Math.Min(page, totalPages);
        return View(new ProductIndexViewModel { Products = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(), CurrentPage = page, TotalPages = totalPages, TotalCount = count });
    }

    public async Task<IActionResult> Create() { await SetCategoriesAsync(); return View(new ProductInputViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductInputViewModel model)
    {
        if (!await IsValidCategoryAsync(model.CategoryId)) ModelState.AddModelError(nameof(model.CategoryId), "Select one of your active categories.");
        if (!ModelState.IsValid) { await SetCategoriesAsync(); return View(model); }
        string? image = null;
        try
        {
            image = await SaveImageAsync(model.Image);
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            db.Products.Add(new Product { Name = model.Name.Trim(), Description = model.Description?.Trim(), Price = model.Price, CategoryId = model.CategoryId!.Value, ImagePath = image, ProductCode = await codes.NextAsync(DateTime.UtcNow), OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow });
            await db.SaveChangesAsync(); await transaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException)
        { DeleteImage(image); ModelState.AddModelError(string.Empty, "The product could not be saved. Please try again."); await SetCategoriesAsync(); return View(model); }
        TempData["Success"] = "Product created."; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId);
        if (product is null) return NotFound(); await SetCategoriesAsync();
        return View(new ProductInputViewModel { ProductId = product.ProductId, Name = product.Name, Description = product.Description, Price = product.Price, CategoryId = product.CategoryId, ExistingImagePath = product.ImagePath, RowVersion = Convert.ToBase64String(product.RowVersion) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductInputViewModel model)
    {
        if (id != model.ProductId) return BadRequest();
        if (!await IsValidCategoryAsync(model.CategoryId)) ModelState.AddModelError(nameof(model.CategoryId), "Select one of your active categories.");
        if (!ModelState.IsValid) { await SetCategoriesAsync(); return View(model); }
        var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId);
        if (product is null) return NotFound(); string? replacement = null;
        try
        {
            replacement = await SaveImageAsync(model.Image);
            product.Name = model.Name.Trim(); product.Description = model.Description?.Trim(); product.Price = model.Price; product.CategoryId = model.CategoryId!.Value; product.UpdatedBy = UserId; product.UpdatedDate = DateTime.UtcNow;
            if (replacement is not null) product.ImagePath = replacement;
            db.Entry(product).Property(p => p.RowVersion).OriginalValue = Convert.FromBase64String(model.RowVersion ?? "");
            await db.SaveChangesAsync(); if (replacement is not null) DeleteImage(model.ExistingImagePath);
        }
        catch (DbUpdateConcurrencyException) { DeleteImage(replacement); ModelState.AddModelError(string.Empty, "This product was changed by another request. Reload and try again."); await SetCategoriesAsync(); return View(model); }
        catch (InvalidOperationException ex) { DeleteImage(replacement); ModelState.AddModelError(nameof(model.Image), ex.Message); await SetCategoriesAsync(); return View(model); }
        TempData["Success"] = "Product updated."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId); if (product is null) return NotFound();
        var image = product.ImagePath; db.Products.Remove(product); await db.SaveChangesAsync(); DeleteImage(image);
        TempData["Success"] = "Product deleted."; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Export()
    {
        var products = await db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.OwnerId == UserId).OrderBy(p => p.Name).ToListAsync();
        return File(excel.Export(products), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"products-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file is null || file.Length == 0) { TempData["Error"] = "Choose an Excel file to import."; return RedirectToAction(nameof(Index)); }
        if (Path.GetExtension(file.FileName).ToLowerInvariant() != ".xlsx") { TempData["Error"] = "Only .xlsx files are supported."; return RedirectToAction(nameof(Index)); }
        try
        {
            var rows = excel.Read(file);
            if (rows.Count == 0 || rows.Count > 500) throw new InvalidOperationException("The file must contain between 1 and 500 product rows.");
            var categories = await db.Categories.Where(c => c.OwnerId == UserId && c.IsActive).ToDictionaryAsync(c => c.CategoryCode, StringComparer.OrdinalIgnoreCase);
            var invalid = rows.Select((r, i) => new { r, i }).FirstOrDefault(x => x.r.Price <= 0 || string.IsNullOrWhiteSpace(x.r.Name) || !categories.ContainsKey(x.r.CategoryCode));
            if (invalid is not null) throw new InvalidOperationException($"Row {invalid.i + 2} has an invalid name, price, or active category code.");
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            foreach (var row in rows) db.Products.Add(new Product { Name = row.Name, Description = row.Description, Price = row.Price, CategoryId = categories[row.CategoryCode].CategoryId, ProductCode = await codes.NextAsync(DateTime.UtcNow), OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow });
            await db.SaveChangesAsync(); await transaction.CommitAsync(); TempData["Success"] = $"Imported {rows.Count} products.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException) { TempData["Error"] = ex.Message; }
        catch { TempData["Error"] = "The Excel file could not be imported. Check that it is a valid .xlsx workbook."; }
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsValidCategoryAsync(int? categoryId) => categoryId.HasValue && await db.Categories.AnyAsync(c => c.CategoryId == categoryId && c.OwnerId == UserId && c.IsActive);
    private async Task SetCategoriesAsync() => ViewBag.Categories = new SelectList(await db.Categories.AsNoTracking().Where(c => c.OwnerId == UserId && c.IsActive).OrderBy(c => c.Name).ToListAsync(), "CategoryId", "Name");
    private async Task<string?> SaveImageAsync(IFormFile? image)
    {
        if (image is null || image.Length == 0) return null;
        if (image.Length > 5 * 1024 * 1024 || !new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(Path.GetExtension(image.FileName).ToLowerInvariant())) throw new InvalidOperationException("Upload a JPG, PNG, GIF, or WEBP image smaller than 5 MB.");
        var folder = Path.Combine(environment.WebRootPath, "uploads", "product-images"); Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}"; var fullPath = Path.Combine(folder, fileName);
        await using var stream = System.IO.File.Create(fullPath); await image.CopyToAsync(stream); return $"/uploads/product-images/{fileName}";
    }
    private void DeleteImage(string? path) { if (string.IsNullOrWhiteSpace(path)) return; var file = Path.Combine(environment.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)); if (System.IO.File.Exists(file)) System.IO.File.Delete(file); }
}
