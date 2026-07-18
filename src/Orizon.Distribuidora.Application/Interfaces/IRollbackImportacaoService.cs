using Orizon.Distribuidora.Application.Importacoes;
namespace Orizon.Distribuidora.Application.Interfaces;
public interface IRollbackImportacaoService
{
    Task<AnaliseRollbackImportacao> AnalisarAsync(Guid importacaoId,Guid empresaId,Guid? usuarioId,CancellationToken cancellationToken=default);
    Task<ResultadoRollbackImportacao> ExecutarAsync(Guid importacaoId,Guid empresaId,Guid? usuarioId,bool permitirParcial,CancellationToken cancellationToken=default);
    Task<ResultadoRollbackImportacao?> ObterResultadoAsync(Guid importacaoId,Guid empresaId,CancellationToken cancellationToken=default);
    Task<ArquivoExportacaoImportacao> ExportarExcelAsync(Guid importacaoId,Guid empresaId,CancellationToken cancellationToken=default);
    Task<ArquivoExportacaoImportacao> ExportarCsvAsync(Guid importacaoId,Guid empresaId,CancellationToken cancellationToken=default);
}
