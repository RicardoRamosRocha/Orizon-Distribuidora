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
    Task SalvarValidacaoAsync(Guid companyId, Guid importacaoId, Guid? usuarioId, ResultadoValidacaoImportacao resultado, OpcoesValidacaoImportacao opcoes, CancellationToken cancellationToken = default);
}
