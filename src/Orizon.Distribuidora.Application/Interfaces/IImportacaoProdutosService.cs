using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface IImportacaoProdutosService
{
    Task<ImportacaoHistorico> PrepararImportacaoAsync(
        Guid companyId,
        ArquivoImportacaoExcel arquivo,
        Guid? usuarioId = null,
        CancellationToken cancellationToken = default);
}
