using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Orizon.Distribuidora.Application.Products;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class ProductGridExportService : IProductGridExportService
{
    private static readonly string[] DefaultColumns = ["code", "name", "sku", "barcode", "type", "category", "brand", "supplier", "unit", "cost", "price", "margin", "stock", "status"];

    public ProductGridExportResult Export(string format, IReadOnlyList<ProductGridExportRow> rows, IReadOnlyList<string> columns)
    {
        var selected = (columns.Count == 0 ? DefaultColumns : columns.Where(DefaultColumns.Contains).Distinct()).ToArray();
        return format.ToLowerInvariant() switch
        {
            "xlsx" => Excel(rows, selected),
            "csv" => Csv(rows, selected),
            "pdf" => Pdf(rows, selected),
            _ => throw new ArgumentException("Formato de exportacao invalido.", nameof(format))
        };
    }

    private static ProductGridExportResult Excel(IReadOnlyList<ProductGridExportRow> rows, string[] columns)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Produtos");
        for (var c = 0; c < columns.Length; c++) sheet.Cell(1, c + 1).Value = Label(columns[c]);
        for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < columns.Length; c++) sheet.Cell(r + 2, c + 1).Value = Convert.ToString(Value(rows[r], columns[c]), CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty;
        var header = sheet.Range(1, 1, 1, Math.Max(1, columns.Length));
        header.Style.Font.Bold = true; header.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF1FF");
        sheet.SheetView.FreezeRows(1);
        if (rows.Count > 0 && columns.Length > 0) sheet.Range(1, 1, rows.Count + 1, columns.Length).SetAutoFilter();
        sheet.Columns().AdjustToContents(8, 42);
        using var stream = new MemoryStream(); workbook.SaveAs(stream);
        return new(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx");
    }

    private static ProductGridExportResult Csv(IReadOnlyList<ProductGridExportRow> rows, string[] columns)
    {
        var text = new StringBuilder();
        text.AppendLine(string.Join(';', columns.Select(x => Escape(Label(x)))));
        foreach (var row in rows) text.AppendLine(string.Join(';', columns.Select(x => Escape(Convert.ToString(Value(row, x), CultureInfo.GetCultureInfo("pt-BR")) ?? ""))));
        var encoding = new UTF8Encoding(true);
        return new(encoding.GetPreamble().Concat(encoding.GetBytes(text.ToString())).ToArray(), "text/csv; charset=utf-8", "csv");
    }

    private static ProductGridExportResult Pdf(IReadOnlyList<ProductGridExportRow> rows, string[] columns)
    {
        var data = rows.Select(row => string.Join(" | ", columns.Select(c => Convert.ToString(Value(row, c), CultureInfo.InvariantCulture) ?? ""))).ToArray();
        var chunks = data.Chunk(68).DefaultIfEmpty([]).ToArray();
        var fontId = 3 + chunks.Length * 2;
        var objects = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", string.Empty };
        var pageIds = new List<int>();
        for (var page = 0; page < chunks.Length; page++)
        {
            var pageId = 3 + page * 2;
            var contentId = pageId + 1;
            pageIds.Add(pageId);
            var content = new StringBuilder("BT /F1 8 Tf 28 810 Td 11 TL ");
            content.Append("(GRADE DE PRODUTOS - Pagina ").Append(page + 1).Append(" de ").Append(chunks.Length).Append(") Tj T* ");
            content.Append('(').Append(PdfEscape(Ascii(string.Join(" | ", columns.Select(Label)), 145))).Append(") Tj T* ");
            foreach (var line in chunks[page]) content.Append('(').Append(PdfEscape(Ascii(line, 145))).Append(") Tj T* ");
            content.Append("ET");
            var length = Encoding.ASCII.GetByteCount(content.ToString());
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentId} 0 R >>");
            objects.Add($"<< /Length {length} >>\nstream\n{content}\nendstream");
        }
        objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', pageIds.Select(x => $"{x} 0 R"))}] /Count {pageIds.Count} >>";
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        using var stream = new MemoryStream(); using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine("%PDF-1.4"); writer.Flush(); var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++) { offsets.Add(stream.Position); writer.WriteLine($"{i + 1} 0 obj"); writer.WriteLine(objects[i]); writer.WriteLine("endobj"); writer.Flush(); }
        var xref = stream.Position; writer.WriteLine("xref"); writer.WriteLine($"0 {objects.Count + 1}"); writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1)) writer.WriteLine($"{offset:0000000000} 00000 n ");
        writer.WriteLine($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>"); writer.WriteLine("startxref"); writer.WriteLine(xref); writer.WriteLine("%%EOF"); writer.Flush();
        return new(stream.ToArray(), "application/pdf", "pdf");
    }

    private static object Value(ProductGridExportRow r, string c) => c switch { "code" => r.Code, "name" => r.Name, "sku" => r.Sku ?? "", "barcode" => r.Barcode ?? "", "type" => r.Type, "category" => r.Category ?? "", "brand" => r.Brand ?? "", "supplier" => r.Supplier ?? "", "unit" => r.Unit, "cost" => r.Cost, "price" => r.Price, "margin" => r.Margin, "stock" => r.MinimumStock ?? 0, "status" => r.Active ? "Ativo" : "Inativo", _ => "" };
    private static string Label(string c) => c switch { "code" => "Codigo", "name" => "Descricao", "sku" => "SKU", "barcode" => "Codigo de barras", "type" => "Tipo", "category" => "Categoria", "brand" => "Marca", "supplier" => "Fornecedor", "unit" => "Unidade", "cost" => "Custo", "price" => "Preco", "margin" => "Margem (%)", "stock" => "Estoque minimo", "status" => "Status", _ => c };
    private static string Escape(string value) => value.ContainsAny(';', '"', '\r', '\n') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    private static string PdfEscape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string Ascii(string value, int length) => new(value.Normalize(NormalizationForm.FormD).Where(c => c <= 127).Take(length).ToArray());
}

file static class StringExportExtensions
{
    public static bool ContainsAny(this string value, params char[] chars) => value.IndexOfAny(chars) >= 0;
}
