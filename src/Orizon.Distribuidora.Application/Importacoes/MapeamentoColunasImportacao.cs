namespace Orizon.Distribuidora.Application.Importacoes;

public enum TipoCampoImportacao { Texto, Inteiro, Decimal, Booleano }

public sealed record CampoImportavel(
    string Chave, string Nome, string Descricao, bool Obrigatorio,
    TipoCampoImportacao Tipo, bool AceitaImportacao, bool AceitaAtualizacao,
    IReadOnlyList<string> Sinonimos,
    string Destino,
    string? MotivoIndisponibilidade = null);

public sealed record SugestaoMapeamento(string Campo, string Coluna, double Confianca);

public sealed record MapeamentoColunasImportacao(
    IReadOnlyDictionary<string, string> Colunas,
    IReadOnlyDictionary<string, double>? Confiancas = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Conflitos = null)
{
    public static IReadOnlyDictionary<string, string> Canonicalizar(
        IReadOnlyDictionary<string, string> colunas)
    {
        var catalogo = CatalogoCamposProdutoImportacao.Campos
            .ToDictionary(campo => campo.Chave, StringComparer.OrdinalIgnoreCase);
        var resultado = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var item in colunas.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
        {
            if (catalogo.TryGetValue(item.Key, out var campo))
            {
                resultado[campo.Chave] = item.Value;
            }
            else
            {
                resultado[item.Key] = item.Value;
            }
        }

        return resultado;
    }
}

public sealed record ErroMapeamento(string Campo, string Mensagem);

public sealed record ResultadoValidacaoMapeamento(IReadOnlyList<ErroMapeamento> Erros)
{
    public bool Valido => Erros.Count == 0;
}
