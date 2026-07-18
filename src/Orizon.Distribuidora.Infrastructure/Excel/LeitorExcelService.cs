using ClosedXML.Excel;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;

namespace Orizon.Distribuidora.Infrastructure.Excel;

public sealed class LeitorExcelService : ILeitorExcelService
{
    private const int MaxSampleRows = 10_000;

    public Task<PlanilhaImportada> LerAsync(
        ArquivoImportacaoExcel arquivo,
        string? abaSelecionada = null,
        int tamanhoAmostra = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arquivo);

        if (arquivo.Conteudo.CanSeek)
        {
            arquivo.Conteudo.Position = 0;
        }

        try
        {
            using var workbook = new XLWorkbook(arquivo.Conteudo);
            var worksheets = workbook.Worksheets.ToList();

            if (worksheets.Count == 0)
            {
                throw new ImportacaoExcelException("O arquivo Excel não possui planilhas.");
            }

            var sampleSize = Math.Clamp(tamanhoAmostra, 0, MaxSampleRows);
            var abas = new List<AbaPlanilhaImportada>();

            foreach (var worksheet in worksheets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var aba = LerAba(worksheet, sampleSize);

                if (aba is not null)
                {
                    abas.Add(aba);
                }
            }

            if (abas.Count == 0)
            {
                throw new ImportacaoExcelException("O arquivo Excel não possui nenhuma planilha com dados válidos.");
            }

            var selectedSheet = ResolverAbaSelecionada(abas, abaSelecionada);
            return Task.FromResult(new PlanilhaImportada(selectedSheet.Nome, abas));
        }
        catch (ImportacaoExcelException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or FormatException)
        {
            throw new ImportacaoExcelException("Não foi possível abrir o arquivo Excel. Verifique se o arquivo .xlsx é válido e não está corrompido.", exception);
        }
    }

    private static AbaPlanilhaImportada? LerAba(IXLWorksheet worksheet, int sampleSize)
    {
        var usedRange = worksheet.RangeUsed();

        if (usedRange is null)
        {
            return null;
        }

        var headerRow = usedRange.Rows()
            .FirstOrDefault(row => row.CellsUsed().Any(cell => !string.IsNullOrWhiteSpace(cell.GetFormattedString())));

        if (headerRow is null)
        {
            return null;
        }

        var firstColumn = usedRange.FirstColumn().ColumnNumber();
        var lastColumn = Math.Max(usedRange.LastColumn().ColumnNumber(), headerRow.LastCellUsed()?.Address.ColumnNumber ?? firstColumn);
        var headers = BuildHeaders(headerRow, firstColumn, lastColumn);

        if (headers.Count == 0)
        {
            throw new ImportacaoExcelException($"A planilha '{worksheet.Name}' não possui cabeçalhos válidos.");
        }

        var sampleRows = new List<LinhaPlanilhaImportada>();
        var dataRowCount = 0;

        foreach (var row in usedRange.Rows().Where(row => row.RowNumber() > headerRow.RowNumber()))
        {
            if (!HasData(row, firstColumn, lastColumn))
            {
                continue;
            }

            dataRowCount++;

            if (sampleRows.Count < sampleSize)
            {
                sampleRows.Add(BuildSampleRow(row, headers, firstColumn));
            }
        }

        if (dataRowCount == 0)
        {
            throw new ImportacaoExcelException($"A planilha '{worksheet.Name}' não possui linhas de dados.");
        }

        return new AbaPlanilhaImportada(
            worksheet.Name,
            worksheet.Position,
            headers,
            dataRowCount,
            sampleRows);
    }

    private static List<string> BuildHeaders(IXLRangeRow headerRow, int firstColumn, int lastColumn)
    {
        var headers = new List<string>();

        for (var column = firstColumn; column <= lastColumn; column++)
        {
            var value = headerRow.Cell(column).GetFormattedString().Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                value = $"Coluna {column}";
            }

            headers.Add(value);
        }

        return headers
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .ToList();
    }

    private static bool HasData(IXLRangeRow row, int firstColumn, int lastColumn)
    {
        for (var column = firstColumn; column <= lastColumn; column++)
        {
            if (!string.IsNullOrWhiteSpace(row.Cell(column).GetFormattedString()))
            {
                return true;
            }
        }

        return false;
    }

    private static LinhaPlanilhaImportada BuildSampleRow(
        IXLRangeRow row,
        IReadOnlyList<string> headers,
        int firstColumn)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < headers.Count; index++)
        {
            var value = row.Cell(firstColumn + index).GetFormattedString().Trim();
            values[headers[index]] = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return new LinhaPlanilhaImportada(row.RowNumber(), values);
    }

    private static AbaPlanilhaImportada ResolverAbaSelecionada(
        IReadOnlyList<AbaPlanilhaImportada> abas,
        string? abaSelecionada)
    {
        if (string.IsNullOrWhiteSpace(abaSelecionada))
        {
            return abas.OrderBy(aba => aba.Ordem).First();
        }

        return abas.FirstOrDefault(aba => string.Equals(aba.Nome, abaSelecionada, StringComparison.OrdinalIgnoreCase))
            ?? throw new ImportacaoExcelException($"A planilha '{abaSelecionada}' não foi encontrada no arquivo Excel.");
    }
}
