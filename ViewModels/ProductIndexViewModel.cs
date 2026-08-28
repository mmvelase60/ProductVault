using ProductVault.Models;

namespace ProductVault.ViewModels;

public class ProductIndexViewModel
{
    public IReadOnlyList<Product> Products { get; init; } = Array.Empty<Product>();
    public IReadOnlyList<Category> Categories { get; init; } = Array.Empty<Category>();
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public string Sort { get; init; } = "newest";
}
