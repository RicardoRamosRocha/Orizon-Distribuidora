namespace Orizon.Distribuidora.Application.Importacoes;

public static class ValidadorMapeamentoColunas
{
    public static ResultadoValidacaoMapeamento Validar(IReadOnlyDictionary<string, string> mapeamentos, IReadOnlyList<string> cabecalhos, IReadOnlyList<IReadOnlyDictionary<string, string?>>? amostra = null)
    {
        var erros = new List<ErroMapeamento>();
        var campos = CatalogoCamposProdutoImportacao.Campos;
        foreach (var campo in campos.Where(x => x.Obrigatorio && (!mapeamentos.TryGetValue(x.Chave, out var coluna) || string.IsNullOrWhiteSpace(coluna))))
            erros.Add(new(campo.Chave, $"O campo obrigatório {campo.Nome} não foi mapeado."));

        foreach (var item in mapeamentos.Where(x => !string.IsNullOrWhiteSpace(x.Value) && !cabecalhos.Contains(x.Value, StringComparer.Ordinal)))
            erros.Add(new(item.Key, $"A coluna '{item.Value}' não existe na planilha."));

        foreach (var grupo in mapeamentos.Where(x => !string.IsNullOrWhiteSpace(x.Value)).GroupBy(x => x.Value, StringComparer.Ordinal).Where(x => x.Count() > 1))
            foreach (var item in grupo) erros.Add(new(item.Key, $"A coluna '{grupo.Key}' está sendo usada mais de uma vez."));

        if (amostra is not null)
        {
            foreach (var item in mapeamentos)
            {
                var campo = campos.FirstOrDefault(x => x.Chave == item.Key);
                if (campo is null || campo.Tipo == TipoCampoImportacao.Texto) continue;
                var valores = amostra.Select(x => x.TryGetValue(item.Value, out var value) ? value : null).Where(x => !string.IsNullOrWhiteSpace(x)).Take(20).ToList();
                var invalidos = valores.Count(value => !Compativel(value!, campo.Tipo));
                if (valores.Count > 0 && invalidos > valores.Count / 2)
                    erros.Add(new(item.Key, $"A coluna '{item.Value}' parece incompatível com o tipo {campo.Tipo}."));
            }
        }

        return new(erros);
    }


    private static bool Compativel(string valor, TipoCampoImportacao tipo) => tipo switch
    {
        TipoCampoImportacao.Inteiro => int.TryParse(valor, out _),
        TipoCampoImportacao.Decimal => decimal.TryParse(valor, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-BR"), out _) || decimal.TryParse(valor, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _),
        TipoCampoImportacao.Booleano => new[] { "sim", "não", "nao", "s", "n", "true", "false", "1", "0" }.Contains(valor.Trim().ToLowerInvariant()),
        _ => true
    };
}
