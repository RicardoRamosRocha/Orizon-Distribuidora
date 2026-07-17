namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record PlanilhaImportada(
    IReadOnlyList<string> Cabecalhos,
    IReadOnlyList<LinhaPlanilhaImportada> Linhas);
