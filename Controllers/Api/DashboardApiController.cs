using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize, Route("api/dashboard")]
public class DashboardApiController(ApplicationDbContext db, UserManager<IdentityUser> users) : ControllerBase
{
    private string UserId => users.GetUserId(User)!;

    [HttpGet]
    public async Task<DashboardResponse> Get()
    {
        var products = db.Products.AsNoTracking().Where(product => product.OwnerId == UserId);
        var categories = db.Categories.AsNoTracking().Where(category => category.OwnerId == UserId);
        var recent = await products.Include(product => product.Category).OrderByDescending(product => product.CreatedDate).Take(5)
            .Select(product => new RecentProductResponse(product.ProductId, product.Name, product.ProductCode, product.Price, product.Category!.Name, product.ImagePath)).ToListAsync();
        return new DashboardResponse(
            await products.CountAsync(),
            await categories.CountAsync(category => category.IsActive),
            await categories.CountAsync(),
            await products.SumAsync(product => (decimal?)product.Price) ?? 0,
            recent);
    }
}

public sealed record DashboardResponse(int ProductCount, int ActiveCategoryCount, int TotalCategoryCount, decimal CatalogueValue, IReadOnlyList<RecentProductResponse> RecentProducts);
public sealed record RecentProductResponse(int ProductId, string Name, string ProductCode, decimal Price, string CategoryName, string? ImagePath);
