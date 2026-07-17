using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;

namespace Orizon.Distribuidora.Infrastructure.Excel;

public sealed class LeitorExcelService : ILeitorExcelService
{
    public Task<PlanilhaImportada> LerAsync(
        ArquivoImportacaoExcel arquivo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arquivo);

        return Task.FromResult(new PlanilhaImportada([], []));
    }
}
