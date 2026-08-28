using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ProductVault.ViewModels;

public class ProductInputViewModel
{
    public int ProductId { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    [Required, Range(typeof(decimal), "0.01", "999999999.99")] public decimal Price { get; set; }
    [Required, Display(Name = "Category")] public int? CategoryId { get; set; }
    [Display(Name = "Product image")] public IFormFile? Image { get; set; }
    public string? ExistingImagePath { get; set; }
    public string? RowVersion { get; set; }
}
