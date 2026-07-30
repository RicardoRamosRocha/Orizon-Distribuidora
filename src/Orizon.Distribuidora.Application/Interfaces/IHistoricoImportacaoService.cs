using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface IHistoricoImportacaoService
{
    Task<ImportacaoHistorico> RegistrarAsync(
        Guid companyId,
        ArquivoImportacaoExcel arquivo,
        Guid? usuarioId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportacaoHistorico>> ListarAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<ImportacaoHistorico?> ObterAsync(
        Guid companyId,
        Guid importacaoId,
        CancellationToken cancellationToken = default);
    Task CancelarAsync(Guid companyId, Guid importacaoId, Guid? usuarioId, CancellationToken cancellationToken = default);
    Task AssociarModeloAsync(Guid companyId, Guid importacaoId, Guid modeloId, Guid? usuarioId, CancellationToken cancellationToken = default);
    Task<PaginaValidacaoImportacao?> ObterValidacaoAsync(Guid companyId, Guid importacaoId, string? filtro, string? busca, int pagina, int tamanhoPagina = 50, CancellationToken cancellationToken = default);
    Task SalvarValidacaoAsync(Guid companyId, Guid importacaoId, Guid? usuarioId, ResultadoValidacaoImportacao resultado, OpcoesValidacaoImportacao opcoes, CancellationToken cancellationToken = default);
    Task<PaginaHistoricoImportacao> ConsultarAsync(Guid companyId,ConsultaHistoricoImportacao consulta,CancellationToken cancellationToken=default);
    Task<HistoricoImportacaoDetalhesDto?> ObterDetalhesAsync(Guid companyId,Guid importacaoId,CancellationToken cancellationToken=default);
    Task<HistoricoDashboardDto> ObterDashboardAsync(Guid companyId,DateTimeOffset? inicio=null,DateTimeOffset? fim=null,CancellationToken cancellationToken=default);
    Task<Guid> DuplicarConfiguracaoAsync(Guid companyId,Guid importacaoId,Guid? usuarioId,CancellationToken cancellationToken=default);
    Task ExcluirHistoricoAsync(Guid companyId,Guid importacaoId,Guid? usuarioId,CancellationToken cancellationToken=default);
}
