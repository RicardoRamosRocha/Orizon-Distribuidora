namespace Orizon.Distribuidora.Application.Interfaces;

public interface IHeaderSynonymProvider
{
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllSynonymsAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetSynonymsAsync(
        string campoDestino,
        Guid? companyId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetLearnedSynonymKeysAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    void Invalidate(Guid? companyId = null);
}
