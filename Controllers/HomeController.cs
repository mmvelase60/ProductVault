using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.ViewModels;

namespace ProductVault.Controllers;

public class HomeController(ApplicationDbContext db, UserManager<IdentityUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new DashboardViewModel());
        }

        var userId = userManager.GetUserId(User)!;
        var products = db.Products.AsNoTracking().Where(product => product.OwnerId == userId);
        var categories = db.Categories.AsNoTracking().Where(category => category.OwnerId == userId);

        return View(new DashboardViewModel
        {
            ProductCount = await products.CountAsync(),
            CatalogueValue = await products.SumAsync(product => (decimal?)product.Price) ?? 0,
            TotalCategoryCount = await categories.CountAsync(),
            ActiveCategoryCount = await categories.CountAsync(category => category.IsActive),
            RecentProducts = await products
                .Include(product => product.Category)
                .OrderByDescending(product => product.CreatedDate)
                .Take(5)
                .ToListAsync()
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
