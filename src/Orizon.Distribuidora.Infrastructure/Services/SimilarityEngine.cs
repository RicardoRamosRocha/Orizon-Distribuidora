using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class SimilarityEngine : ISimilarityEngine
{
    private static readonly IReadOnlyList<CampoImportavel> KnownFields =
        CatalogoCamposProdutoImportacao.Campos.Where(field => field.AceitaImportacao).ToList();

    public IReadOnlyList<RecognitionResult> Recognize(
        string header,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? additionalSynonyms = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        var normalizedHeader = HeaderSynonymDictionary.Normalize(header);
        var direct = KnownFields
            .Select(field => DirectRecognition(field, header, normalizedHeader, additionalSynonyms))
            .Where(result => result is not null)
            .Select(result => result!)
            .OrderByDescending(result => result.Strategy == RecognitionStrategy.Exact)
            .ThenBy(result => result.CampoDestino, StringComparer.Ordinal)
            .ToList();
        if (direct.Count > 0) return direct;

        return KnownFields
            .Select(field => new RecognitionResult(
                field.Chave,
                Math.Round(BestSimilarity(field, normalizedHeader, additionalSynonyms) * 100, 2),
                RecognitionStrategy.Similarity,
                header))
            .OrderByDescending(result => result.Confidence)
            .ThenBy(result => result.CampoDestino, StringComparer.Ordinal)
            .ToList();
    }

    private static RecognitionResult? DirectRecognition(
        CampoImportavel field,
        string header,
        string normalizedHeader,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? additionalSynonyms)
    {
        if (new[] { field.Chave, field.Nome }.Any(term => HeaderSynonymDictionary.Normalize(term) == normalizedHeader))
            return new(field.Chave, 100, RecognitionStrategy.Exact, header);

        return Synonyms(field, additionalSynonyms).Any(term => HeaderSynonymDictionary.Normalize(term) == normalizedHeader)
            ? new(field.Chave, 100, RecognitionStrategy.Synonym, header)
            : null;
    }

    private static double BestSimilarity(
        CampoImportavel field,
        string normalizedHeader,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? additionalSynonyms) =>
        new[] { field.Chave, field.Nome }
            .Concat(Synonyms(field, additionalSynonyms))
            .Select(term => Similarity(HeaderSynonymDictionary.Normalize(term), normalizedHeader))
            .Max();

    private static IEnumerable<string> Synonyms(
        CampoImportavel field,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? additionalSynonyms) =>
        HeaderSynonymDictionary.GetSynonyms(field.Chave)
            .Concat(field.Sinonimos)
            .Concat(additionalSynonyms?.GetValueOrDefault(field.Chave, []) ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase);

    private static double Similarity(string left, string right)
    {
        if (left == right) return 1;
        if (left.Length == 0 || right.Length == 0) return 0;
        var containment = (left.Length >= 4 && right.Contains(left, StringComparison.Ordinal)) ||
            (right.Length >= 4 && left.Contains(right, StringComparison.Ordinal)) ? .9 : 0;
        return Math.Max(containment, Math.Max(Levenshtein(left, right), JaroWinkler(left, right)));
    }

    private static double Levenshtein(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        for (var i = 1; i <= left.Length; i++)
        {
            var current = new int[right.Length + 1];
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1));
            previous = current;
        }

        return 1d - (double)previous[right.Length] / Math.Max(left.Length, right.Length);
    }

    private static double JaroWinkler(string left, string right)
    {
        var matchDistance = Math.Max(0, Math.Max(left.Length, right.Length) / 2 - 1);
        var leftMatches = new bool[left.Length];
        var rightMatches = new bool[right.Length];
        var matches = 0;

        for (var i = 0; i < left.Length; i++)
        {
            var start = Math.Max(0, i - matchDistance);
            var end = Math.Min(i + matchDistance + 1, right.Length);
            for (var j = start; j < end; j++)
            {
                if (rightMatches[j] || left[i] != right[j]) continue;
                leftMatches[i] = true;
                rightMatches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0) return 0;
        var transpositions = 0;
        for (int i = 0, j = 0; i < left.Length; i++)
        {
            if (!leftMatches[i]) continue;
            while (!rightMatches[j]) j++;
            if (left[i] != right[j]) transpositions++;
            j++;
        }

        var jaro = ((double)matches / left.Length +
            (double)matches / right.Length +
            (matches - transpositions / 2d) / matches) / 3d;
        var prefix = 0;
        while (prefix < Math.Min(4, Math.Min(left.Length, right.Length)) && left[prefix] == right[prefix]) prefix++;
        return jaro + prefix * .1 * (1 - jaro);
    }
}
