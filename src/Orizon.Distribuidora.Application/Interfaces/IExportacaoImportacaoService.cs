using Orizon.Distribuidora.Application.Importacoes;
namespace Orizon.Distribuidora.Application.Interfaces;
public interface IExportacaoImportacaoService
{
    Task<ArquivoExportacaoImportacao> ExportarExcelAsync(Guid importacaoId,Guid empresaId,Guid? usuarioId,FiltroExportacaoImportacao filtro,CancellationToken cancellationToken=default);
    Task<ArquivoExportacaoImportacao> ExportarCsvAsync(Guid importacaoId,Guid empresaId,Guid? usuarioId,FiltroExportacaoImportacao filtro,CancellationToken cancellationToken=default);
}
