using System.Text;
using ClosedXML.Excel;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Enums;
namespace Orizon.Distribuidora.Infrastructure.Services;

public static class ExportacaoImportacaoRenderer
{
    private static readonly string[] Headers=["Linha","Código","Descrição","Código de Barras","Tipo Produto","Operação","Status","Mensagem","Campo","Código do Erro","Produto Relacionado","Empresa","Usuário","Data","Valor Original","Valor Persistido","Observações"];
    public static ArquivoExportacaoImportacao Excel(DadosExportacaoImportacao data,string fileName)
    {
        using var workbook=new XLWorkbook();var summary=workbook.Worksheets.Add("Resumo");var entries=new (string,object? )[]{("Arquivo",data.Resumo.Arquivo),("Empresa",data.Resumo.Empresa),("Usuário",data.Resumo.Usuario),("Data da importação",data.Resumo.DataImportacao.LocalDateTime),("Data da execução",data.Resumo.DataExecucao?.LocalDateTime),("Duração",data.Resumo.Duracao?.ToString()),("Total de linhas",data.Resumo.Total),("Inseridos",data.Resumo.Inseridos),("Atualizados",data.Resumo.Atualizados),("Sem alteração",data.Resumo.SemAlteracao),("Ignorados",data.Resumo.Ignorados),("Bloqueados",data.Resumo.Bloqueados),("Falhas",data.Resumo.Falhas),("Status final",data.Resumo.Status.ToString()),("Modelo utilizado",data.Resumo.Modelo??"Sem modelo")};
        for(var i=0;i<entries.Length;i++){summary.Cell(i+1,1).Value=entries[i].Item1;summary.Cell(i+1,1).Style.Font.Bold=true;summary.Cell(i+1,2).Value=XLCellValue.FromObject(entries[i].Item2??"");}summary.Columns().AdjustToContents();
        var sheet=workbook.Worksheets.Add("Dados");for(var c=0;c<Headers.Length;c++){sheet.Cell(1,c+1).Value=Headers[c];sheet.Cell(1,c+1).Style.Font.Bold=true;sheet.Cell(1,c+1).Style.Fill.BackgroundColor=XLColor.FromHtml("#E2E8F0");}
        for(var r=0;r<data.Linhas.Count;r++){var row=data.Linhas[r];var values=Values(row);for(var c=0;c<values.Length;c++)sheet.Cell(r+2,c+1).Value=Safe(values[c]);var color=row.PossuiErro||row.Status is StatusLinhaImportacao.Falhou or StatusLinhaImportacao.Bloqueada?"#FECACA":row.PossuiAviso?"#FEF3C7":row.Status==StatusLinhaImportacao.Inserida?"#DCFCE7":row.Status==StatusLinhaImportacao.Atualizada?"#DBEAFE":"#E5E7EB";sheet.Row(r+2).Style.Fill.BackgroundColor=XLColor.FromHtml(color);}
        sheet.SheetView.FreezeRows(1);sheet.Range(1,1,Math.Max(1,data.Linhas.Count+1),Headers.Length).SetAutoFilter();sheet.Columns().AdjustToContents(1,60);using var stream=new MemoryStream();workbook.SaveAs(stream);return new(stream.ToArray(),fileName,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
    public static ArquivoExportacaoImportacao Csv(DadosExportacaoImportacao data,string fileName)
    {using var stream=new MemoryStream();using(var writer=new StreamWriter(stream,new UTF8Encoding(true),1024,true)){writer.WriteLine(string.Join(';',Headers.Select(Escape)));foreach(var row in data.Linhas)writer.WriteLine(string.Join(';',Values(row).Select(x=>Escape(Safe(x)))));}return new(stream.ToArray(),fileName,"text/csv; charset=utf-8");}
    private static string[] Values(LinhaExportacaoImportacao x)=>[x.Linha.ToString(),x.Codigo??"",x.Descricao??"",x.CodigoBarras??"",x.TipoProduto??"",x.Operacao.ToString(),x.Status.ToString(),x.Mensagem??"",x.Campo??"",x.CodigoErro??"",x.ProdutoId?.ToString()??"",x.Empresa,x.Usuario,x.Data.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"),x.ValorOriginal,x.ValorPersistido??"",x.Observacoes??""];
    private static string Safe(string value)=>value.Length>0&&"=+-@".Contains(value[0])?"'"+value:value;
    private static string Escape(string value)=>value.IndexOfAny([';','"','\r','\n'])>=0?"\""+value.Replace("\"","\"\"")+"\"":value;
}
