namespace Orizon.Distribuidora.Application.Importacoes;

public enum TipoCampoImportacao { Texto, Inteiro, Decimal, Booleano }

public sealed record CampoImportavel(
    string Chave, string Nome, string Descricao, bool Obrigatorio,
    TipoCampoImportacao Tipo, bool AceitaImportacao, bool AceitaAtualizacao,
    IReadOnlyList<string> Sinonimos);

public sealed record SugestaoMapeamento(string Campo, string Coluna, double Confianca);

public sealed record MapeamentoColunasImportacao(
    IReadOnlyDictionary<string, string> Colunas,
    IReadOnlyDictionary<string, double>? Confiancas = null);

public sealed record ErroMapeamento(string Campo, string Mensagem);

public sealed record ResultadoValidacaoMapeamento(IReadOnlyList<ErroMapeamento> Erros)
{
    public bool Valido => Erros.Count == 0;
}
