using System.Text;
using ClosedXML.Excel;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ExportacaoImportacaoTests
{
    [Fact] public void Gera_excel_com_resumo_e_dados(){var file=ExportacaoImportacaoRenderer.Excel(Data([Line()]),"x.xlsx");using var wb=new XLWorkbook(new MemoryStream(file.Conteudo));Assert.Equal(2,wb.Worksheets.Count);Assert.Equal("Resumo",wb.Worksheet(1).Name);Assert.Equal("Dados",wb.Worksheet(2).Name);Assert.Equal("Arquivo",wb.Worksheet("Resumo").Cell("A1").GetString());}
    [Fact] public void Excel_possui_autofiltro_e_cabecalho_congelado(){var file=ExportacaoImportacaoRenderer.Excel(Data([Line()]),"x.xlsx");using var wb=new XLWorkbook(new MemoryStream(file.Conteudo));var ws=wb.Worksheet("Dados");Assert.True(ws.AutoFilter.IsEnabled);Assert.Equal(1,ws.SheetView.SplitRow);Assert.True(ws.Cell(1,1).Style.Font.Bold);}
    [Fact] public void Gera_csv_utf8_com_bom_e_separador(){var file=ExportacaoImportacaoRenderer.Csv(Data([Line(description:"Café; Premium")]),"x.csv");Assert.Equal([0xEF,0xBB,0xBF],file.Conteudo.Take(3));var text=Encoding.UTF8.GetString(file.Conteudo);Assert.Contains("Linha;Código;Descrição",text);Assert.Contains("\"Café; Premium\"",text);}
    [Fact] public void Exportacao_vazia_mantem_cabecalho(){var file=ExportacaoImportacaoRenderer.Csv(Data([]),"x.csv");Assert.Single(Encoding.UTF8.GetString(file.Conteudo).Trim().Split('\n'));}
    [Fact] public void Exportacao_grande_preserva_dez_mil_linhas(){var rows=Enumerable.Range(1,10_000).Select(x=>Line(x)).ToList();var file=ExportacaoImportacaoRenderer.Csv(Data(rows),"x.csv");Assert.Equal(10_001,Encoding.UTF8.GetString(file.Conteudo).Split('\n',StringSplitOptions.RemoveEmptyEntries).Length);}
    [Fact] public void Preserva_mensagem_e_valores_originais(){var line=Line(message:"Erro amigável") with{ValorOriginal="{\"Código\":\"001\"}",ValorPersistido="{\"codigo\":\"001\"}"};var text=Encoding.UTF8.GetString(ExportacaoImportacaoRenderer.Csv(Data([line]),"x.csv").Conteudo);Assert.Contains("Erro amigável",text);Assert.Contains("Código",text);Assert.Contains("codigo",text);}
    [Fact] public void Protege_contra_formula_em_csv(){var text=Encoding.UTF8.GetString(ExportacaoImportacaoRenderer.Csv(Data([Line(description:"=CMD()")]),"x.csv").Conteudo);Assert.Contains("'=CMD()",text);}
    [Fact] public void Filtro_seleciona_status_corretos(){Assert.True(ExportacaoImportacaoService.Matches(Line(status:StatusLinhaImportacao.Inserida),FiltroExportacaoImportacao.Inseridos));Assert.False(ExportacaoImportacaoService.Matches(Line(status:StatusLinhaImportacao.Atualizada),FiltroExportacaoImportacao.Inseridos));}
    [Fact] public void Filtros_de_aviso_e_erro_sao_independentes(){Assert.True(ExportacaoImportacaoService.Matches(Line() with{PossuiAviso=true},FiltroExportacaoImportacao.Avisos));Assert.True(ExportacaoImportacaoService.Matches(Line() with{PossuiErro=true},FiltroExportacaoImportacao.Erros));}
    [Fact] public void Resumo_contem_contadores(){var file=ExportacaoImportacaoRenderer.Excel(Data([Line()]),"x.xlsx");using var wb=new XLWorkbook(new MemoryStream(file.Conteudo));var ws=wb.Worksheet("Resumo");Assert.Equal("Inseridos",ws.Cell("A8").GetString());Assert.Equal(1,ws.Cell("B8").GetValue<int>());}
    [Fact] public void Linha_com_erro_recebe_cor_vermelha(){var file=ExportacaoImportacaoRenderer.Excel(Data([Line() with{PossuiErro=true}]),"x.xlsx");using var wb=new XLWorkbook(new MemoryStream(file.Conteudo));Assert.Equal(XLColor.FromHtml("#FECACA"),wb.Worksheet("Dados").Cell(2,1).Style.Fill.BackgroundColor);}

    private static DadosExportacaoImportacao Data(IReadOnlyList<LinhaExportacaoImportacao> lines)=>new(new(Guid.NewGuid(),"produtos.xlsx","Orizon","Usuário",DateTimeOffset.Now,DateTimeOffset.Now,TimeSpan.FromSeconds(2),lines.Count,1,0,0,0,0,0,StatusImportacao.Concluida,"Padrão"),lines);
    private static LinhaExportacaoImportacao Line(int number=1,string description="Produto",string? message=null,StatusLinhaImportacao status=StatusLinhaImportacao.Inserida)=>new(number,"001",description,"0001","Próprio",OperacaoExecucaoImportacao.Inserir,status,message,null,null,Guid.NewGuid(),"Orizon","Usuário",DateTimeOffset.Now,"{}","{}",null,false,false);
}
