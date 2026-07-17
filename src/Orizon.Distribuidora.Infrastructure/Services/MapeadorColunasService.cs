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
        var usadas = new HashSet<string>(StringComparer.Ordinal);

        foreach (var campo in CatalogoCamposProdutoImportacao.Campos)
        {
            var melhor = cabecalhos.Where(x => !usadas.Contains(x))
                .Select(x => (Coluna: x, Nota: MelhorNota(campo, x)))
                .OrderByDescending(x => x.Nota).FirstOrDefault();
            if (melhor.Nota < .72) continue;
            resultado[campo.Chave] = melhor.Coluna;
            confiancas[campo.Chave] = melhor.Nota;
            usadas.Add(melhor.Coluna);
        }
        return Task.FromResult(new MapeamentoColunasImportacao(resultado, confiancas));
    }

    public static string Normalizar(string valor)
    {
        var decomposed = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)) builder.Append(c);
        return builder.ToString();
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
