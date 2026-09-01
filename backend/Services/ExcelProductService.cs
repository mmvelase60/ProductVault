using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using ProductVault.Models;

namespace ProductVault;

public sealed class ExcelProductService : IExcelProductService
{
    public byte[] Export(IEnumerable<Product> products)
    {
        using var workbook = new XLWorkbook(); var sheet = workbook.Worksheets.Add("Products");
        var headers = new[] { "Product Code", "Name", "Description", "Category Code", "Category", "Price", "Quantity In Stock", "Reorder Level", "Created Date" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Row(1).Style.Font.Bold = true; var row = 2;
        foreach (var p in products) { sheet.Cell(row, 1).Value = p.ProductCode; sheet.Cell(row, 2).Value = p.Name; sheet.Cell(row, 3).Value = p.Description; sheet.Cell(row, 4).Value = p.Category?.CategoryCode; sheet.Cell(row, 5).Value = p.Category?.Name; sheet.Cell(row, 6).Value = p.Price; sheet.Cell(row, 7).Value = p.QuantityInStock; sheet.Cell(row, 8).Value = p.ReorderLevel; sheet.Cell(row, 9).Value = p.CreatedDate; row++; }
        sheet.Column(6).Style.NumberFormat.Format = "#,##0.00"; sheet.Column(9).Style.DateFormat.Format = "yyyy-mm-dd HH:mm"; sheet.Columns().AdjustToContents();
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

    public IReadOnlyList<CatalogueImportRow> ReadCatalogue(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension == ".xlsx")
        {
            using var stream = file.OpenReadStream(); using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("The workbook contains no worksheet.");
            return ReadCatalogueRows(sheet.RowsUsed().Select(row => Enumerable.Range(1, 8).Select(column => row.Cell(column).GetString()).ToArray()));
        }

        if (extension == ".csv")
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var lines = new List<string[]>(); string? line;
            while ((line = reader.ReadLine()) is not null) lines.Add(ParseCsvLine(line).ToArray());
            return ReadCatalogueRows(lines);
        }

        throw new InvalidOperationException("Choose a CSV or Excel (.xlsx) catalogue file.");
    }

    private static IReadOnlyList<CatalogueImportRow> ReadCatalogueRows(IEnumerable<string[]> source)
    {
        var rows = source.ToList();
        var headers = new[] { "Category Name", "Category Code", "Category Active", "Product Name", "Description", "Price" };
        if (rows.Count == 0 || rows[0].Length < headers.Length || !headers.Select((header, index) => string.Equals(header, rows[0][index].Trim(), StringComparison.OrdinalIgnoreCase)).All(valid => valid))
            throw new InvalidOperationException("Use these columns: Category Name, Category Code, Category Active, Product Name, Description, Price.");

        var result = new List<CatalogueImportRow>();
        var hasStockColumns = rows[0].Length >= 8 && string.Equals(rows[0][6].Trim(), "Quantity In Stock", StringComparison.OrdinalIgnoreCase) && string.Equals(rows[0][7].Trim(), "Reorder Level", StringComparison.OrdinalIgnoreCase);
        if (rows[0].Length > 6 && !hasStockColumns)
            throw new InvalidOperationException("Optional stock columns must be named Quantity In Stock and Reorder Level.");

        foreach (var (row, index) in rows.Skip(1).Select((row, index) => (row, index)))
        {
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            if (row.Length < headers.Length) throw new InvalidOperationException("Each import row must contain all six columns.");
            var priceValid = decimal.TryParse(row[5], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var price);
            var activeValid = bool.TryParse(row[2], out var active);
            var quantity = 0;
            var reorderLevel = 0;
            var quantityValid = !hasStockColumns || int.TryParse(row.ElementAtOrDefault(6), out quantity);
            var reorderValid = !hasStockColumns || int.TryParse(row.ElementAtOrDefault(7), out reorderLevel);
            result.Add(new CatalogueImportRow(index + 2, row[0].Trim(), row[1].Trim().ToUpperInvariant(), active, activeValid, row[3].Trim(), string.IsNullOrWhiteSpace(row[4]) ? null : row[4].Trim(), price, priceValid, hasStockColumns ? quantity : 0, quantityValid, hasStockColumns ? reorderLevel : 0, reorderValid));
        }
        return result;
    }

    private static IEnumerable<string> ParseCsvLine(string line)
    {
        var value = new System.Text.StringBuilder(); var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            if (line[index] == '"' && index + 1 < line.Length && line[index + 1] == '"') { value.Append('"'); index++; }
            else if (line[index] == '"') quoted = !quoted;
            else if (line[index] == ',' && !quoted) { yield return value.ToString(); value.Clear(); }
            else value.Append(line[index]);
        }
        if (quoted) throw new InvalidOperationException("The CSV contains an unclosed quoted value.");
        yield return value.ToString();
    }
}
