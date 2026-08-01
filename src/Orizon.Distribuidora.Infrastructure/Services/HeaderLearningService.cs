using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class HeaderLearningService(
    ApplicationDbContext dbContext,
    IHeaderSynonymProvider synonymProvider,
    ISimilarityEngine similarityEngine) : IHeaderLearningService
{
    public const int LearnedPriority = -100;
    public const string LearnedOrigin = "Learned";

    public async Task<int> LearnAsync(
        Guid companyId,
        Guid? userId,
        IReadOnlyDictionary<string, string> confirmedMappings,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty) throw new ArgumentException("A empresa é obrigatória.", nameof(companyId));
        ArgumentNullException.ThrowIfNull(confirmedMappings);
        if (confirmedMappings.Count == 0) return 0;

        var mappings = MapeamentoColunasImportacao.Canonicalizar(confirmedMappings);
        var validFields = CatalogoCamposProdutoImportacao.Campos.Where(field => field.AceitaImportacao)
            .Select(field => field.Chave)
            .ToHashSet(StringComparer.Ordinal);
        mappings = mappings.Where(item => validFields.Contains(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        if (mappings.Count == 0) return 0;

        var synonyms = await synonymProvider.GetAllSynonymsAsync(companyId, cancellationToken);
        var fields = mappings.Keys.ToList();
        var persisted = await dbContext.HeaderSynonyms.IgnoreQueryFilters().AsNoTracking()
            .Where(item => !item.IsDeleted && fields.Contains(item.CampoDestino) &&
                (item.CompanyId == null || item.CompanyId == companyId))
            .Select(item => new { item.CampoDestino, item.Sinonimo })
            .ToListAsync(cancellationToken);
        var equivalents = persisted
            .Select(item => Key(item.CampoDestino, item.Sinonimo))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var field in fields)
            foreach (var synonym in synonyms.GetValueOrDefault(field, []))
                equivalents.Add(Key(field, synonym));

        var learned = new List<HeaderSynonym>();
        foreach (var mapping in mappings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = Key(mapping.Key, mapping.Value);
            if (equivalents.Contains(key)) continue;

            var recognition = similarityEngine.Recognize(mapping.Value, synonyms)
                .FirstOrDefault(result => result.CampoDestino == mapping.Key);
            if (recognition?.Strategy is RecognitionStrategy.Exact or RecognitionStrategy.Synonym) continue;

            learned.Add(new HeaderSynonym(
                mapping.Key,
                mapping.Value,
                LearnedPriority,
                LearnedOrigin,
                companyId,
                ativo: true)
            {
                CreatedBy = userId
            });
            equivalents.Add(key);
        }

        if (learned.Count == 0) return 0;
        dbContext.HeaderSynonyms.AddRange(learned);
        await dbContext.SaveChangesAsync(cancellationToken);
        synonymProvider.Invalidate(companyId);
        return learned.Count;
    }

    private static string Key(string field, string synonym) =>
        $"{field}:{HeaderSynonymDictionary.Normalize(synonym)}";
}
