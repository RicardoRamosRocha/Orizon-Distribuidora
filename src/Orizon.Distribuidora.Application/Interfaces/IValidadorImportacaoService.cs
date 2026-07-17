using Orizon.Distribuidora.Application.Importacoes;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface IValidadorImportacaoService
{
    Task<ResultadoValidacaoImportacao> ValidarAsync(
        PlanilhaImportada planilha,
        MapeamentoColunasImportacao mapeamento,
        CancellationToken cancellationToken = default);
}
