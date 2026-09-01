using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductVault.Models;

public class Product : AuditableEntity
{
    public int ProductId { get; set; }

    [Required, StringLength(10)]
    [Display(Name = "Product code")]
    public string ProductCode { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Quantity in stock")]
    public int QuantityInStock { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Reorder level")]
    public int ReorderLevel { get; set; }

    public string? ImagePath { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
