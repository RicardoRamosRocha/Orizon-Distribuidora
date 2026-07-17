using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class MapeadorColunasService : IMapeadorColunasService
{
    public Task<MapeamentoColunasImportacao> MapearAsync(
        IReadOnlyList<string> cabecalhos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cabecalhos);

        return Task.FromResult(new MapeamentoColunasImportacao(new Dictionary<string, string>()));
    }
}
