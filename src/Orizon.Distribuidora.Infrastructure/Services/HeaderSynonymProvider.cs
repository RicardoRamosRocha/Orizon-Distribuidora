using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class HeaderSynonymProvider(ApplicationDbContext dbContext, IMemoryCache cache)
    : IHeaderSynonymProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<string>> GetSynonymsAsync(
        string campoDestino,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(campoDestino))
            throw new ArgumentException("O campo de destino é obrigatório.", nameof(campoDestino));

        var field = campoDestino.Trim();
        var all = await GetAllSynonymsAsync(companyId, cancellationToken);
        return all.GetValueOrDefault(field, []);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllSynonymsAsync(
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(companyId);
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            var databaseSynonyms = await dbContext.HeaderSynonyms.AsNoTracking()
                .Where(item => item.Ativo && (item.CompanyId == null || item.CompanyId == companyId))
                .OrderByDescending(item => item.CompanyId == companyId && companyId != null)
                .ThenByDescending(item => item.Prioridade)
                .ThenBy(item => item.CreatedAt)
                .Select(item => new { item.CampoDestino, item.Sinonimo })
                .ToListAsync(cancellationToken);

            var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            var fields = CatalogoCamposProdutoImportacao.Campos.Where(item => item.AceitaImportacao)
                .Select(item => item.Chave)
                .Concat(databaseSynonyms.Select(item => item.CampoDestino))
                .Distinct(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                var values = new List<string>();
                var normalized = new HashSet<string>(StringComparer.Ordinal);
                foreach (var synonym in databaseSynonyms.Where(item => item.CampoDestino == field).Select(item => item.Sinonimo)
                    .Concat(HeaderSynonymDictionary.GetSynonyms(field)))
                {
                    if (normalized.Add(HeaderSynonymDictionary.Normalize(synonym))) values.Add(synonym);
                }
                result[field] = values;
            }

            return (IReadOnlyDictionary<string, IReadOnlyList<string>>)result;
        }) ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
    }

    public async Task<IReadOnlySet<string>> GetLearnedSynonymKeysAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = LearnedCacheKey(companyId);
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var learned = await dbContext.HeaderSynonyms.AsNoTracking()
                .Where(item => item.Ativo && item.CompanyId == companyId && item.Origem == HeaderLearningService.LearnedOrigin)
                .Select(item => new { item.CampoDestino, item.Sinonimo })
                .ToListAsync(cancellationToken);
            return (IReadOnlySet<string>)learned.Select(item => Key(item.CampoDestino, item.Sinonimo))
                .ToHashSet(StringComparer.Ordinal);
        }) ?? new HashSet<string>(StringComparer.Ordinal);
    }

    public void Invalidate(Guid? companyId = null)
    {
        cache.Remove(CacheKey(companyId));
        if (companyId.HasValue) cache.Remove(LearnedCacheKey(companyId.Value));
    }

    private static string CacheKey(Guid? companyId) =>
        $"header-synonyms:{companyId?.ToString("N") ?? "global"}";

    private static string LearnedCacheKey(Guid companyId) => $"header-synonyms-learned:{companyId:N}";
    private static string Key(string field, string synonym) => $"{field}:{HeaderSynonymDictionary.Normalize(synonym)}";
}
