using Orizon.Distribuidora.Application.Importacoes;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface IMapeadorColunasService
{
    Task<MapeamentoColunasImportacao> MapearAsync(
        IReadOnlyList<string> cabecalhos,
        CancellationToken cancellationToken = default);
}

public interface IModeloImportacaoService
{
    Task<IReadOnlyList<ModeloImportacaoDto>> ListarAsync(Guid empresaId, Guid? usuarioId, CancellationToken cancellationToken = default);
    Task<ModeloImportacaoDto?> EncontrarCompativelAsync(Guid empresaId, Guid? usuarioId, IReadOnlyList<string> cabecalhos, CancellationToken cancellationToken = default);
    Task<ModeloImportacaoDto> SalvarAsync(Guid empresaId, Guid? usuarioId, string nome, bool padrao, IReadOnlyDictionary<string, string> mapeamentos, IReadOnlyList<string> cabecalhos, CancellationToken cancellationToken = default);
    Task ExcluirAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default);
}

public sealed record ModeloImportacaoDto(Guid Id, string Nome, Guid? UsuarioId, DateTimeOffset Data, bool Padrao, IReadOnlyDictionary<string, string> Mapeamentos);
