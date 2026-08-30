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

    private static byte[] CreateWorkbook()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Products");
        sheet.Cell(1, 1).Value = "Name"; sheet.Cell(1, 2).Value = "Description"; sheet.Cell(1, 3).Value = "Category Code"; sheet.Cell(1, 4).Value = "Price";
        sheet.Cell(2, 1).Value = "Wireless mouse"; sheet.Cell(2, 2).Value = "Bluetooth"; sheet.Cell(2, 3).Value = "ACC001"; sheet.Cell(2, 4).Value = 249.99m;
        using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
    }
}
