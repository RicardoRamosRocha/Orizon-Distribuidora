namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record MapeamentoColunasImportacao(
    IReadOnlyDictionary<string, string> Colunas);
