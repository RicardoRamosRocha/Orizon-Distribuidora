using Orizon.Distribuidora.Application.Importacoes;
namespace Orizon.Distribuidora.Application.Interfaces;
public interface IExecutorImportacaoProdutosService
{
    Task<ResultadoExecucaoImportacao> ExecutarAsync(Guid importacaoId,Guid empresaId,Guid? usuarioId,CancellationToken cancellationToken=default);
    Task<ResultadoExecucaoImportacao?> ObterResultadoAsync(Guid importacaoId,Guid empresaId,CancellationToken cancellationToken=default);
    Task<PaginaResultadoExecucao?> ObterResultadoPaginaAsync(Guid importacaoId, Guid empresaId, string? filtro, string? busca, int pagina, int tamanhoPagina = 50, CancellationToken cancellationToken = default);
}
