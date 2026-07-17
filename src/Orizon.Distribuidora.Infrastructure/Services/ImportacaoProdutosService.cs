using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class ImportacaoProdutosService : IImportacaoProdutosService
{
    private readonly IHistoricoImportacaoService historicoImportacaoService;

    public ImportacaoProdutosService(IHistoricoImportacaoService historicoImportacaoService)
    {
        this.historicoImportacaoService = historicoImportacaoService;
    }

    public Task<ImportacaoHistorico> PrepararImportacaoAsync(
        Guid companyId,
        ArquivoImportacaoExcel arquivo,
        CancellationToken cancellationToken = default)
    {
        return historicoImportacaoService.RegistrarAsync(companyId, arquivo, cancellationToken);
    }
}
