using System.ComponentModel.DataAnnotations;
using ProductVault.Models;

namespace ProductVault.Tests;

public class CategoryValidationTests
{
    [Theory]
    [InlineData("ABC123", true)]
    [InlineData("abc123", true)]
    [InlineData("AB1234", false)]
    [InlineData("ABCD12", false)]
    [InlineData("ABC12", false)]
    public void CategoryCode_requires_three_letters_followed_by_three_numbers(string code, bool expectedValid)
    {
        var category = new Category { Name = "Accessories", CategoryCode = code, OwnerId = "user-1", CreatedBy = "user-1", CreatedDate = DateTime.UtcNow };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(category, new ValidationContext(category), results, validateAllProperties: true);

        Assert.Equal(expectedValid, isValid);
    }
}
