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
public sealed record CatalogueImportRow(int RowNumber, string CategoryName, string CategoryCode, bool CategoryActive, bool HasValidCategoryActive, string ProductName, string? Description, decimal Price, bool HasValidPrice, int QuantityInStock, bool HasValidQuantity, int ReorderLevel, bool HasValidReorderLevel);
