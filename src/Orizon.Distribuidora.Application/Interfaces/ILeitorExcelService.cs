using Orizon.Distribuidora.Application.Importacoes;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface ILeitorExcelService
{
    Task<PlanilhaImportada> LerAsync(
        ArquivoImportacaoExcel arquivo,
        CancellationToken cancellationToken = default);
}
