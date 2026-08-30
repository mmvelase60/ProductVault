using System.ComponentModel.DataAnnotations;

namespace ProductVault.Models;

public class Category : AuditableEntity
{
    public int CategoryId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(6), RegularExpression("^[A-Za-z]{3}[0-9]{3}$", ErrorMessage = "Category code must be 3 letters followed by 3 numbers (for example, ABC123).")]
    [Display(Name = "Category code")]
    public string CategoryCode { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
