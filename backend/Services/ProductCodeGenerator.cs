using Microsoft.EntityFrameworkCore;
using ProductVault.Data;

namespace ProductVault;

public sealed class ProductCodeGenerator(ApplicationDbContext db) : IProductCodeGenerator
{
    public async Task<string> NextAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var prefix = utcNow.ToString("yyyyMM-");
        var latest = await db.Products.AsNoTracking().Where(p => p.ProductCode.StartsWith(prefix))
            .OrderByDescending(p => p.ProductCode).Select(p => p.ProductCode).FirstOrDefaultAsync(cancellationToken);
        var sequence = latest is null ? 1 : int.Parse(latest[7..]) + 1;
        if (sequence > 999) throw new InvalidOperationException("The monthly product-code limit has been reached.");
        return $"{prefix}{sequence:000}";
    }
}
