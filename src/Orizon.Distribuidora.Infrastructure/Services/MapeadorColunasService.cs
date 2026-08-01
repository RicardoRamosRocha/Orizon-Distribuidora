using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class MapeadorColunasService : IMapeadorColunasService
{
    private readonly ISimilarityEngine similarityEngine;
    private readonly IHeaderSynonymProvider? synonymProvider;

    public MapeadorColunasService() : this(new SimilarityEngine(), null)
    {
    }

    public MapeadorColunasService(ISimilarityEngine similarityEngine, IHeaderSynonymProvider? synonymProvider)
    {
        this.similarityEngine = similarityEngine;
        this.synonymProvider = synonymProvider;
    }

    public Task<MapeamentoColunasImportacao> MapearAsync(
        IReadOnlyList<string> cabecalhos,
        CancellationToken cancellationToken = default) =>
        MapearAsync(cabecalhos, null, cancellationToken);

    public Task<MapeamentoColunasImportacao> MapearAsync(
        IReadOnlyList<string> cabecalhos,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        MapearAsync(cabecalhos, (Guid?)companyId, cancellationToken);

    private async Task<MapeamentoColunasImportacao> MapearAsync(
        IReadOnlyList<string> cabecalhos,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cabecalhos);
        var synonyms = synonymProvider is null
            ? null
            : await synonymProvider.GetAllSynonymsAsync(companyId, cancellationToken);
        var resultado = new Dictionary<string, string>();
        var confiancas = new Dictionary<string, double>();
        var conflitos = new Dictionary<string, IReadOnlyList<string>>();
        var propostas = new List<(CampoImportavel Campo, string Coluna, double Nota, RecognitionResult Recognition)>();
        var recognitionByField = new Dictionary<string, RecognitionResult>(StringComparer.Ordinal);
        var reconhecimentos = cabecalhos
            .Select(header => (Header: header, Results: similarityEngine.Recognize(header, synonyms)))
            .ToList();

        foreach (var campo in CatalogoCamposProdutoImportacao.Campos.Where(campo => campo.AceitaImportacao))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidatas = reconhecimentos
                .SelectMany(item => item.Results
                    .Where(result => result.CampoDestino == campo.Chave)
                    .Select(result => (Coluna: item.Header, Nota: result.Confidence / 100d, Recognition: result)))
                .Where(x => x.Nota >= .72)
                .OrderByDescending(x => x.Nota)
                .ToList();
            if (candidatas.Count == 0) continue;

            var melhorNota = candidatas[0].Nota;
            var melhores = candidatas
                .Where(x => Math.Abs(x.Nota - melhorNota) < .0001)
                .Select(x => x.Coluna)
                .ToList();
            if (melhores.Count > 1)
            {
                conflitos[campo.Chave] = melhores;
                continue;
            }

            var best = candidatas.First(item => item.Coluna == melhores[0]);
            propostas.Add((campo, best.Coluna, melhorNota, best.Recognition));
        }

        foreach (var grupo in propostas.GroupBy(x => x.Coluna, StringComparer.Ordinal))
        {
            var melhorNota = grupo.Max(x => x.Nota);
            var melhores = grupo.Where(x => Math.Abs(x.Nota - melhorNota) < .0001).ToList();
            if (melhores.Count > 1)
            {
                foreach (var proposta in melhores)
                {
                    conflitos[proposta.Campo.Chave] = [proposta.Coluna];
                }
                continue;
            }

            var melhor = melhores[0];
            resultado[melhor.Campo.Chave] = melhor.Coluna;
            confiancas[melhor.Campo.Chave] = melhor.Nota;
            recognitionByField[melhor.Campo.Chave] = melhor.Recognition;
        }

        return new MapeamentoColunasImportacao(resultado, confiancas, conflitos, recognitionByField);
    }

    public static string Normalizar(string valor) => HeaderSynonymDictionary.Normalize(valor);
}
