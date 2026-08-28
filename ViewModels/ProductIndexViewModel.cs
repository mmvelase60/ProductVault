using ProductVault.Models;

namespace ProductVault.ViewModels;

public class ProductIndexViewModel
{
    public IReadOnlyList<Product> Products { get; init; } = Array.Empty<Product>();
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
}
