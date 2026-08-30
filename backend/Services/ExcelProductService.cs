using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using ProductVault.Models;

namespace ProductVault;

public sealed class ExcelProductService : IExcelProductService
{
    public byte[] Export(IEnumerable<Product> products)
    {
        using var workbook = new XLWorkbook(); var sheet = workbook.Worksheets.Add("Products");
        var headers = new[] { "Product Code", "Name", "Description", "Category Code", "Category", "Price", "Created Date" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true; var row = 2;
        foreach (var p in products) { sheet.Cell(row, 1).Value = p.ProductCode; sheet.Cell(row, 2).Value = p.Name; sheet.Cell(row, 3).Value = p.Description; sheet.Cell(row, 4).Value = p.Category?.CategoryCode; sheet.Cell(row, 5).Value = p.Category?.Name; sheet.Cell(row, 6).Value = p.Price; sheet.Cell(row, 7).Value = p.CreatedDate; row++; }
        sheet.Column(6).Style.NumberFormat.Format = "#,##0.00"; sheet.Column(7).Style.DateFormat.Format = "yyyy-mm-dd HH:mm"; sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream(); workbook.SaveAs(stream); return stream.ToArray();
    }

    public IReadOnlyList<ExcelProductRow> Read(IFormFile file)
    {
        using var stream = file.OpenReadStream(); using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("The workbook contains no worksheet."); var rows = new List<ExcelProductRow>();
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var name = row.Cell(1).GetString().Trim(); if (string.IsNullOrEmpty(name)) continue;
            var description = row.Cell(2).GetString().Trim(); var categoryCode = row.Cell(3).GetString().Trim().ToUpperInvariant();
            if (!row.Cell(4).TryGetValue<decimal>(out var price)) throw new InvalidOperationException($"Row {row.RowNumber()}: Price must be a number.");
            rows.Add(new ExcelProductRow(name, string.IsNullOrWhiteSpace(description) ? null : description, categoryCode, price));
        }
        return rows;
    }
}
