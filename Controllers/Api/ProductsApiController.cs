using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize, Route("api/products")]
public class ProductsApiController(ApplicationDbContext db, UserManager<IdentityUser> users, IProductCodeGenerator codes) : ControllerBase
{
    private string UserId => users.GetUserId(User)!;

    [HttpGet]
    public async Task<ProductPageResponse> Get(int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.OwnerId == UserId).OrderByDescending(p => p.CreatedDate);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(p => new ProductResponse(p.ProductId, p.ProductCode, p.Name, p.Description, p.Price, p.CategoryId, p.Category!.Name, p.ImagePath)).ToListAsync();
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
        return CreatedAtAction(nameof(Get), new ProductResponse(product.ProductId, product.ProductCode, product.Name, product.Description, product.Price, category.CategoryId, category.Name, product.ImagePath));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0) return BadRequest(new { errors = new { product = "Name is required and price must be greater than zero." } });
        var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId); if (product is null) return NotFound();
        var category = await db.Categories.SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.OwnerId == UserId && c.IsActive); if (category is null) return BadRequest(new { errors = new { categoryId = "Choose an active category you own." } });
        product.Name = request.Name.Trim(); product.Description = request.Description?.Trim(); product.Price = request.Price; product.CategoryId = category.CategoryId; product.UpdatedBy = UserId; product.UpdatedDate = DateTime.UtcNow; await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    { var product = await db.Products.SingleOrDefaultAsync(p => p.ProductId == id && p.OwnerId == UserId); if (product is null) return NotFound(); db.Products.Remove(product); await db.SaveChangesAsync(); return NoContent(); }
}

public sealed record ProductRequest(string Name, string? Description, decimal Price, int CategoryId);
public sealed record ProductResponse(int ProductId, string ProductCode, string Name, string? Description, decimal Price, int CategoryId, string CategoryName, string? ImagePath);
public sealed record ProductPageResponse(IReadOnlyList<ProductResponse> Items, int Page, int PageSize, int TotalCount);
