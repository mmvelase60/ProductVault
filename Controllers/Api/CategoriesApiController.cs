using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductVault.Data;
using ProductVault.Models;

namespace ProductVault.Controllers.Api;

[ApiController, Authorize, Route("api/categories")]
public class CategoriesApiController(ApplicationDbContext db, UserManager<IdentityUser> users) : ControllerBase
{
    private string UserId => users.GetUserId(User)!;

    [HttpGet]
    public Task<List<CategoryResponse>> Get() => db.Categories.AsNoTracking().Where(c => c.OwnerId == UserId).OrderBy(c => c.Name).Select(c => new CategoryResponse(c.CategoryId, c.Name, c.CategoryCode, c.IsActive)).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
    {
        var code = request.CategoryCode.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z]{3}[0-9]{3}$")) return BadRequest(new { errors = new { categoryCode = "Use 3 letters followed by 3 numbers (ABC123)." } });
        if (await db.Categories.AnyAsync(c => c.OwnerId == UserId && c.CategoryCode == code)) return Conflict(new { message = "This category code is already in use." });
        var category = new Category { Name = request.Name.Trim(), CategoryCode = code, IsActive = request.IsActive, OwnerId = UserId, CreatedBy = UserId, CreatedDate = DateTime.UtcNow };
        db.Categories.Add(category); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new CategoryResponse(category.CategoryId, category.Name, category.CategoryCode, category.IsActive));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await db.Categories.SingleOrDefaultAsync(c => c.CategoryId == id && c.OwnerId == UserId); if (category is null) return NotFound();
        var code = request.CategoryCode.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Z]{3}[0-9]{3}$")) return BadRequest(new { message = "Category code must follow ABC123." });
        if (await db.Categories.AnyAsync(c => c.OwnerId == UserId && c.CategoryCode == code && c.CategoryId != id)) return Conflict(new { message = "This category code is already in use." });
        category.Name = request.Name.Trim(); category.CategoryCode = code; category.IsActive = request.IsActive; category.UpdatedBy = UserId; category.UpdatedDate = DateTime.UtcNow; await db.SaveChangesAsync(); return NoContent();
    }
}

public sealed record CategoryRequest(string Name, string CategoryCode, bool IsActive = true);
public sealed record CategoryResponse(int CategoryId, string Name, string CategoryCode, bool IsActive);
