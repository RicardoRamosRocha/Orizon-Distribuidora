using System.Globalization;
using System.Text;

namespace Orizon.Distribuidora.Application.Importacoes;

public static class HeaderSynonymDictionary
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Synonyms =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["codigo"] = ["codigo", "código", "cod", "cód", "sku", "referencia", "referência", "cod produto", "codigo produto", "código do produto", "product code"],
            ["descricao"] = ["descricao", "descrição", "nome", "produto", "descricao produto", "descrição do produto", "nome produto", "product name"],
            ["unidade"] = ["unidade", "un", "und", "unid", "unidade medida", "un medida", "unidade de medida", "unit"],
            ["precoCompra"] = ["preco compra", "preço compra", "custo", "preço custo", "preco custo", "valor custo", "valor compra", "pc", "purchase price"],
            ["precoVenda"] = ["preco", "preço", "preco venda", "preço venda", "preço de venda", "valor venda", "pv", "venda", "sale price"],
            ["estoqueInicial"] = ["estoque", "estoque inicial", "saldo", "saldo estoque", "quantidade", "qtd estoque", "initial stock"],
            ["marca"] = ["marca", "fabricante", "brand"],
            ["categoria"] = ["categoria", "departamento", "secao", "seção", "category"],
            ["subcategoria"] = ["subcategoria", "sub categoria", "sub-category", "subcategory"],
            ["grupo"] = ["grupo", "grupo produto", "grupo de produto", "familia", "família", "group"],
            ["fornecedor"] = ["fornecedor", "fornecedor principal", "supplier", "vendor"],
            ["parceiroCnpj"] = ["parceiro", "cnpj parceiro", "parceiro cnpj", "documento parceiro", "partner"],
            ["ncm"] = ["ncm", "classificacao fiscal", "classificação fiscal", "codigo ncm", "código ncm"],
            ["observacoes"] = ["observacoes", "observações", "observacao", "observação", "obs", "notas", "notes"]
        };

    public static IReadOnlyList<string> GetSynonyms(string fieldKey) =>
        Synonyms.TryGetValue(fieldKey, out var synonyms) ? synonyms : [];

    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark ||
                character is '.' or '-' or '_' or '/')
                continue;

            if (char.IsWhiteSpace(character))
            {
                if (builder.Length > 0 && !previousWasSpace)
                {
                    builder.Append(' ');
                    previousWasSpace = true;
                }
                continue;
            }

            builder.Append(character);
            previousWasSpace = false;
        }

        return builder.ToString().TrimEnd();
    }
}
