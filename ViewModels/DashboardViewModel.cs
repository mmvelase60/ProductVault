using ProductVault.Models;

namespace ProductVault.ViewModels;

public class DashboardViewModel
{
    public int ProductCount { get; init; }
    public int ActiveCategoryCount { get; init; }
    public int TotalCategoryCount { get; init; }
    public decimal CatalogueValue { get; init; }
    public IReadOnlyList<Product> RecentProducts { get; init; } = Array.Empty<Product>();
}
