using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;
using ProductVault.ViewModels;

namespace ProductVault.Controllers;

[Authorize]
public class CategoriesController(ApplicationDbContext db, UserManager<IdentityUser> userManager) : Controller
{
    private string UserId => userManager.GetUserId(User)!;

    public async Task<IActionResult> Index() => View(await db.Categories.AsNoTracking().Where(c => c.OwnerId == UserId).OrderBy(c => c.Name).ToListAsync());
    public IActionResult Create() => View(new CategoryInputViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryInputViewModel model)
    {
        model.CategoryCode = model.CategoryCode.Trim().ToUpperInvariant();
        if (!ModelState.IsValid) return View(model);
        if (await db.Categories.AnyAsync(c => c.OwnerId == UserId && c.CategoryCode == model.CategoryCode)) { ModelState.AddModelError(nameof(model.CategoryCode), "This category code is already in use."); return View(model); }
        db.Categories.Add(new Category { Name = model.Name.Trim(), CategoryCode = model.CategoryCode, IsActive = model.IsActive, OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow });
        await db.SaveChangesAsync(); TempData["Success"] = "Category created."; return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.CategoryId == id && c.OwnerId == UserId);
        if (category is null) return NotFound();
        return View(new CategoryInputViewModel { CategoryId = category.CategoryId, Name = category.Name, CategoryCode = category.CategoryCode, IsActive = category.IsActive, RowVersion = Convert.ToBase64String(category.RowVersion) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryInputViewModel model)
    {
        if (id != model.CategoryId) return BadRequest(); model.CategoryCode = model.CategoryCode.Trim().ToUpperInvariant();
        if (!ModelState.IsValid) return View(model);
        var category = await db.Categories.SingleOrDefaultAsync(c => c.CategoryId == id && c.OwnerId == UserId);
        if (category is null) return NotFound();
        if (await db.Categories.AnyAsync(c => c.OwnerId == UserId && c.CategoryCode == model.CategoryCode && c.CategoryId != id)) { ModelState.AddModelError(nameof(model.CategoryCode), "This category code is already in use."); return View(model); }
        category.Name = model.Name.Trim(); category.CategoryCode = model.CategoryCode; category.IsActive = model.IsActive; category.UpdatedBy = UserId; category.UpdatedDate = DateTime.UtcNow;
        db.Entry(category).Property(c => c.RowVersion).OriginalValue = Convert.FromBase64String(model.RowVersion ?? "");
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { ModelState.AddModelError(string.Empty, "This category was changed by another request. Reload and try again."); return View(model); }
        TempData["Success"] = "Category updated."; return RedirectToAction(nameof(Index));
    }
}
