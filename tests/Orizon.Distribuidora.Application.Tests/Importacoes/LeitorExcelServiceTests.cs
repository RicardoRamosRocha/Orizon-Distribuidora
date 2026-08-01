using ClosedXML.Excel;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Infrastructure.Excel;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class LeitorExcelServiceTests
{
    [Fact]
    public async Task LerAsync_DeveLerArquivoExcelValido()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = "Código";
            sheet.Cell(1, 2).Value = "Descrição";
            sheet.Cell(2, 1).Value = "P001";
            sheet.Cell(2, 2).Value = "Produto 1";
        });

        var result = await ReadAsync(stream);

        Assert.Equal("Produtos", result.AbaSelecionada);
        Assert.Equal(["Código", "Descrição"], result.Cabecalhos);
        Assert.Single(result.Linhas);
        Assert.Equal("P001", result.Linhas[0].Valores["Código"]);
    }

    [Fact]
    public async Task LerAsync_DeveLerCabecalhos()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = "Código";
            sheet.Cell(1, 2).Value = "Marca";
            sheet.Cell(1, 3).Value = "Preço";
            sheet.Cell(2, 1).Value = "P001";
            sheet.Cell(2, 2).Value = "Orizon";
            sheet.Cell(2, 3).Value = 10;
        });

        var result = await ReadAsync(stream);

        Assert.Equal(["Código", "Marca", "Preço"], result.Cabecalhos);
    }

    [Fact]
    public async Task LerAsync_DeveContarLinhasComDados()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = "Código";
            sheet.Cell(1, 2).Value = "Descrição";
            sheet.Cell(2, 1).Value = "P001";
            sheet.Cell(3, 1).Value = "P002";
            sheet.Cell(5, 2).Value = "Produto 3";
        });

        var result = await ReadAsync(stream);

        Assert.Equal(3, result.AbaAtual!.QuantidadeLinhas);
    }

    [Fact]
    public async Task LerAsync_DeveLerMultiplasAbasESelecionarAbaInformada()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheetA = workbook.Worksheets.Add("Resumo");
            sheetA.Cell(1, 1).Value = "Campo";
            sheetA.Cell(2, 1).Value = "Ignorar";

            var sheetB = workbook.Worksheets.Add("Produtos");
            sheetB.Cell(1, 1).Value = "Código";
            sheetB.Cell(1, 2).Value = "Estoque";
            sheetB.Cell(2, 1).Value = "P001";
            sheetB.Cell(2, 2).Value = 7;
        });

        var result = await ReadAsync(stream, "Produtos");

        Assert.Equal(2, result.Abas.Count);
        Assert.Equal("Produtos", result.AbaSelecionada);
        Assert.Equal(["Código", "Estoque"], result.Cabecalhos);
    }

    [Fact]
    public async Task LerAsync_NaValidacaoDevePercorrerSomenteAbaSelecionada()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var ignored = workbook.Worksheets.Add("Ignorada");
            ignored.Cell(1, 1).Value = "Codigo";
            for (var line = 2; line <= 10_002; line++) ignored.Cell(line, 1).Value = $"I{line}";

            var selected = workbook.Worksheets.Add("Produtos");
            selected.Cell(1, 1).Value = "Codigo";
            selected.Cell(2, 1).Value = "P001";
        });

        var result = await new LeitorExcelService().LerAsync(
            new ArquivoImportacaoExcel(stream, "produtos.xlsx", stream.Length),
            "Produtos",
            tamanhoAmostra: 10_000);

        Assert.Equal("Produtos", result.AbaSelecionada);
        Assert.Single(result.Abas);
        Assert.Single(result.Linhas);
        Assert.Equal("P001", result.Linhas[0].Valores["Codigo"]);
    }

    [Fact]
    public async Task LerAsync_DeveRejeitarWorkbookInvalido()
    {
        await using var stream = new MemoryStream("conteúdo inválido"u8.ToArray());
        var service = new LeitorExcelService();

        var action = () => service.LerAsync(
            new ArquivoImportacaoExcel(stream, "produtos.xlsx", stream.Length));

        var exception = await Assert.ThrowsAsync<ImportacaoExcelException>(action);
        Assert.Contains("não foi possível abrir", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LerAsync_DeveRejeitarPlanilhaVazia()
    {
        using var stream = CreateWorkbook(workbook => workbook.Worksheets.Add("Vazia"));

        var service = new LeitorExcelService();
        var action = () => service.LerAsync(
            new ArquivoImportacaoExcel(stream, "produtos.xlsx", stream.Length));

        var exception = await Assert.ThrowsAsync<ImportacaoExcelException>(action);
        Assert.Contains("planilha com dados válidos", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LerAsync_DeveRejeitarPlanilhaSemLinhasDeDados()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = "Código";
            sheet.Cell(1, 2).Value = "Descrição";
        });

        var service = new LeitorExcelService();
        var action = () => service.LerAsync(
            new ArquivoImportacaoExcel(stream, "produtos.xlsx", stream.Length));

        var exception = await Assert.ThrowsAsync<ImportacaoExcelException>(action);
        Assert.Contains("planilha com dados válidos", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LerAsync_DeveAceitarExatamenteDezMilLinhas()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = "Código";
            for (var line = 2; line <= 10_001; line++) sheet.Cell(line, 1).Value = $"P{line}";
        });

        var result = await new LeitorExcelService().LerAsync(
            new ArquivoImportacaoExcel(stream, "produtos.xlsx", stream.Length), tamanhoAmostra: 10_000);

        Assert.Equal(10_000, result.AbaAtual!.QuantidadeLinhas);
        Assert.Equal(10_000, result.Linhas.Count);
    }

    [Fact]
    public async Task LerAsync_DeveRejeitarDezMilEUmaLinhasSemTruncar()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = "Código";
            for (var line = 2; line <= 10_002; line++) sheet.Cell(line, 1).Value = $"P{line}";
        });

        var exception = await Assert.ThrowsAsync<ImportacaoExcelException>(() =>
            new LeitorExcelService().LerAsync(new ArquivoImportacaoExcel(stream, "produtos.xlsx", stream.Length)));

        Assert.Contains("10.000", exception.Message);
        Assert.Contains("divida", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LerAsync_DeveBloquearCabecalhosDuplicados()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = "Código";
            sheet.Cell(1, 2).Value = "código";
            sheet.Cell(2, 1).Value = "P1";
        });

        var exception = await Assert.ThrowsAsync<ImportacaoExcelException>(() =>
            new LeitorExcelService().LerAsync(new ArquivoImportacaoExcel(stream, "produtos.xlsx", stream.Length)));

        Assert.Contains("cabeçalhos duplicados", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LerAsync_PreservaEspacosAcentosECaixaDoCabecalhoOriginal()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = " Código do Produto ";
            sheet.Cell(1, 2).Value = "PREÇO de Venda";
            sheet.Cell(2, 1).Value = "P1";
            sheet.Cell(2, 2).Value = 10;
        });

        var result = await ReadAsync(stream);

        Assert.Equal([" Código do Produto ", "PREÇO de Venda"], result.Cabecalhos);
        Assert.Equal("P1", result.Linhas[0].Valores[" Código do Produto "]);
    }

    [Fact]
    public async Task LerAsync_DeveIgnorarAbaSomenteComCabecalhoQuandoOutraForValida()
    {
        using var stream = CreateWorkbook(workbook =>
        {
            workbook.Worksheets.Add("Instruções").Cell(1, 1).Value = "Leia antes";
            var sheet = workbook.Worksheets.Add("Produtos");
            sheet.Cell(1, 1).Value = "Código";
            sheet.Cell(2, 1).Value = "P1";
        });

        var result = await ReadAsync(stream);

        Assert.Single(result.Abas);
        Assert.Equal("Produtos", result.AbaSelecionada);
    }

    private static async Task<PlanilhaImportada> ReadAsync(Stream stream, string? sheetName = null)
    {
        var service = new LeitorExcelService();
        return await service.LerAsync(
            new ArquivoImportacaoExcel(stream, "produtos.xlsx", stream.Length),
            sheetName,
            tamanhoAmostra: 5);
    }

    private static MemoryStream CreateWorkbook(Action<XLWorkbook> configure)
    {
        using var workbook = new XLWorkbook();
        configure(workbook);

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
