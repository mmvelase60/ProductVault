using Microsoft.AspNetCore.Http;
using ProductVault.Models;

namespace ProductVault;

public interface IExcelProductService
{
    byte[] Export(IEnumerable<Product> products);
    IReadOnlyList<ExcelProductRow> Read(IFormFile file);
    IReadOnlyList<CatalogueImportRow> ReadCatalogue(IFormFile file);
}

public sealed record ExcelProductRow(string Name, string? Description, string CategoryCode, decimal Price);
public sealed record CatalogueImportRow(string CategoryName, string CategoryCode, bool CategoryActive, string ProductName, string? Description, decimal Price);
