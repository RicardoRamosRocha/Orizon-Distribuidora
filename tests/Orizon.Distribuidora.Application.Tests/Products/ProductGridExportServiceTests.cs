using System.Text;
using ClosedXML.Excel;
using Orizon.Distribuidora.Application.Products;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Products;

public sealed class ProductGridExportServiceTests
{
    private readonly ProductGridExportService service = new();

    [Fact]
    public void Excel_ShouldRespectVisibleColumnsAndRows()
    {
        var result = service.Export("xlsx", Rows(), ["code", "name", "price"]);

        using var workbook = new XLWorkbook(new MemoryStream(result.Content));
        var sheet = workbook.Worksheet("Produtos");
        Assert.Equal("Codigo", sheet.Cell(1, 1).GetString());
        Assert.Equal("Descricao", sheet.Cell(1, 2).GetString());
        Assert.Equal("Preco", sheet.Cell(1, 3).GetString());
        Assert.Equal("PRD-1", sheet.Cell(2, 1).GetString());
        Assert.Equal(3, sheet.RangeUsed()!.ColumnCount());
    }

    [Fact]
    public void Csv_ShouldUseUtf8BomSemicolonAndHeader()
    {
        var result = service.Export("csv", Rows(), ["code", "name"]);
        var text = Encoding.UTF8.GetString(result.Content);

        Assert.Equal(0xEF, result.Content[0]);
        Assert.Contains("Codigo;Descricao", text);
        Assert.Contains("Argamassa", text);
    }

    [Fact]
    public void Pdf_ShouldGenerateValidPdfHeaderAndCatalog()
    {
        var result = service.Export("pdf", Rows(), ["code", "name"]);
        var text = Encoding.ASCII.GetString(result.Content);

        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("/Type /Catalog", text);
        Assert.Contains("PRD-1", text);
    }

    [Theory]
    [InlineData("xlsx")]
    [InlineData("csv")]
    [InlineData("pdf")]
    public void EmptyExport_ShouldStillGenerateFile(string format)
    {
        var result = service.Export(format, [], ["code"]);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public void LargeCsvExport_ShouldPreserveTenThousandRows()
    {
        var rows = Enumerable.Range(1, 10_000)
            .Select(i => Rows()[0] with { Code = $"PRD-{i:00000}" }).ToArray();

        var result = service.Export("csv", rows, ["code", "name"]);
        var lines = Encoding.UTF8.GetString(result.Content).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(10_001, lines.Length);
        Assert.Contains("PRD-10000", lines[^1]);
    }

    private static IReadOnlyList<ProductGridExportRow> Rows() =>
    [new("PRD-1", "Argamassa", "SKU-1", "789", "Proprio", "Construcao", "Orizon", "Fornecedor", "UN", 10, 15, 33.33m, 2, true)];
}
