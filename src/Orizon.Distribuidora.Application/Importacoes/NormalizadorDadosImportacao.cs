namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record ResultadoNormalizacaoImportacao(
    IReadOnlyList<LinhaPlanilhaImportada> Linhas,
    int QuantidadeUnidadesPreenchidasAutomaticamente);

public static class NormalizadorDadosImportacao
{
    public static ResultadoNormalizacaoImportacao Normalizar(
        IReadOnlyList<LinhaPlanilhaImportada> linhas,
        MapeamentoColunasImportacao mapeamento,
        Func<LinhaPlanilhaImportada, bool>? preencherUnidadeVazia = null)
    {
        ArgumentNullException.ThrowIfNull(linhas);
        ArgumentNullException.ThrowIfNull(mapeamento);

        mapeamento.Colunas.TryGetValue("unidade", out var colunaUnidade);
        var quantidadeUnidadesPreenchidas = 0;
        var normalizadas = new List<LinhaPlanilhaImportada>(linhas.Count);

        foreach (var linha in linhas)
        {
            var valores = linha.Valores.ToDictionary(
                item => item.Key,
                item => NormalizarValor(item.Value),
                StringComparer.Ordinal);
            var possuiDados = valores.Values.Any(valor => valor is not null);
            var linhaNormalizada = new LinhaPlanilhaImportada(linha.NumeroLinha, valores);

            if (possuiDados && !string.IsNullOrWhiteSpace(colunaUnidade) &&
                (!valores.TryGetValue(colunaUnidade, out var unidade) || unidade is null) &&
                (preencherUnidadeVazia?.Invoke(linhaNormalizada) ?? true))
            {
                valores[colunaUnidade] = "UN";
                quantidadeUnidadesPreenchidas++;
            }

            normalizadas.Add(linhaNormalizada);
        }

        return new ResultadoNormalizacaoImportacao(normalizadas, quantidadeUnidadesPreenchidas);
    }

    private static string? NormalizarValor(string? valor)
    {
        var normalizado = valor?.Trim();
        return string.IsNullOrEmpty(normalizado) ? null : normalizado;
    }
}
