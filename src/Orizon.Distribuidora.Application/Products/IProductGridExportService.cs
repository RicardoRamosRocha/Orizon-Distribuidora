namespace Orizon.Distribuidora.Application.Products;

public sealed record ProductGridExportRow(
    string Code, string Name, string? Sku, string? Barcode, string Type,
    string? Category, string? Brand, string? Supplier, string Unit,
    decimal Cost, decimal Price, decimal Margin, decimal? MinimumStock, bool Active);

public sealed record ProductGridExportResult(byte[] Content, string ContentType, string Extension);

public interface IProductGridExportService
{
    ProductGridExportResult Export(string format, IReadOnlyList<ProductGridExportRow> rows, IReadOnlyList<string> columns);
}
