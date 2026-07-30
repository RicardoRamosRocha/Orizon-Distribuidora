using System.Globalization;
using System.Text;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class MapeadorColunasService : IMapeadorColunasService
{
    public Task<MapeamentoColunasImportacao> MapearAsync(IReadOnlyList<string> cabecalhos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cabecalhos);
        var resultado = new Dictionary<string, string>();
        var confiancas = new Dictionary<string, double>();
        var conflitos = new Dictionary<string, IReadOnlyList<string>>();
        var propostas = new List<(CampoImportavel Campo, string Coluna, double Nota)>();

        foreach (var campo in CatalogoCamposProdutoImportacao.Campos.Where(campo => campo.AceitaImportacao))
        {
            var candidatas = cabecalhos
                .Select(x => (Coluna: x, Nota: MelhorNota(campo, x)))
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

            propostas.Add((campo, melhores[0], melhorNota));
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
        }

        return Task.FromResult(new MapeamentoColunasImportacao(resultado, confiancas, conflitos));
    }

    public static string Normalizar(string valor)
    {
        var decomposed = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        var ultimoFoiEspaco = false;
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (!ultimoFoiEspaco)
                {
                    builder.Append(' ');
                    ultimoFoiEspaco = true;
                }
                continue;
            }

            builder.Append(c);
            ultimoFoiEspaco = false;
        }

        return builder.ToString().Trim();
    }

    private static double MelhorNota(CampoImportavel campo, string coluna) =>
        new[] { campo.Nome, campo.Chave }.Concat(campo.Sinonimos).Max(x => Similaridade(Normalizar(x), Normalizar(coluna)));

    private static double Similaridade(string a, string b)
    {
        if (a == b) return 1;
        if (a.Length == 0 || b.Length == 0) return 0;
        if ((a.Length >= 4 && b.Contains(a)) || (b.Length >= 4 && a.Contains(b))) return .9;
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1]; current[0] = i;
            for (var j = 1; j <= b.Length; j++) current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            previous = current;
        }
        return 1d - (double)previous[b.Length] / Math.Max(a.Length, b.Length);
    }
}
