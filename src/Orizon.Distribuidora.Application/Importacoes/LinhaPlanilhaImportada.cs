namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record LinhaPlanilhaImportada(
    int NumeroLinha,
    IReadOnlyDictionary<string, string?> Valores);
