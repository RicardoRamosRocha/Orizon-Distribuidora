using Orizon.Distribuidora.Application.Importacoes;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface ILeitorExcelService
{
    Task<PlanilhaImportada> LerAsync(
        ArquivoImportacaoExcel arquivo,
        string? abaSelecionada = null,
        int tamanhoAmostra = 10,
        CancellationToken cancellationToken = default);
}
