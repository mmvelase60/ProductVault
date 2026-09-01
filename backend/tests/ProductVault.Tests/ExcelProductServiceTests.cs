using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;

namespace ProductVault.Tests;

public class ExcelProductServiceTests
{
    [Fact]
    public void Read_reads_the_required_import_columns()
    {
        var content = CreateWorkbook();
        var file = new FormFile(new MemoryStream(content), 0, content.Length, "file", "products.xlsx");

        var rows = new ExcelProductService().Read(file);

        var row = Assert.Single(rows);
        Assert.Equal("Wireless mouse", row.Name);
        Assert.Equal("ACC001", row.CategoryCode);
        Assert.Equal(249.99m, row.Price);
    }

    [Fact]
    public void ReadCatalogue_preserves_invalid_row_values_for_an_error_report()
    {
        const string csv = "Category Name,Category Code,Category Active,Product Name,Description,Price,Quantity In Stock,Reorder Level\nTechnology,TEC202,true,Keyboard,Mechanical,bad-price,-1,5\n";
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "catalogue.csv");

        var row = Assert.Single(new ExcelProductService().ReadCatalogue(file));

        Assert.Equal(2, row.RowNumber);
        Assert.False(row.HasValidPrice);
        Assert.Equal(-1, row.QuantityInStock);
        Assert.True(row.HasValidQuantity);
        Assert.True(row.HasValidReorderLevel);
    }

    private static byte[] CreateWorkbook()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Products");
        sheet.Cell(1, 1).Value = "Name"; sheet.Cell(1, 2).Value = "Description"; sheet.Cell(1, 3).Value = "Category Code"; sheet.Cell(1, 4).Value = "Price";
        sheet.Cell(2, 1).Value = "Wireless mouse"; sheet.Cell(2, 2).Value = "Bluetooth"; sheet.Cell(2, 3).Value = "ACC001"; sheet.Cell(2, 4).Value = 249.99m;
        using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
    }
}
