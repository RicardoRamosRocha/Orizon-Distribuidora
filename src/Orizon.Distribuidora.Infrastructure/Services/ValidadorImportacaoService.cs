using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class ValidadorImportacaoService : IValidadorImportacaoService
{
    public Task<ResultadoValidacaoImportacao> ValidarAsync(
        PlanilhaImportada planilha,
        MapeamentoColunasImportacao mapeamento,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(planilha);
        ArgumentNullException.ThrowIfNull(mapeamento);

        return Task.FromResult(ResultadoValidacaoImportacao.Sucesso);
    }
}
