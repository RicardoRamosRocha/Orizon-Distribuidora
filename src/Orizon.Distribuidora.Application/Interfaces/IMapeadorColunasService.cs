using Orizon.Distribuidora.Application.Importacoes;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface IMapeadorColunasService
{
    Task<MapeamentoColunasImportacao> MapearAsync(
        IReadOnlyList<string> cabecalhos,
        CancellationToken cancellationToken = default);
}
