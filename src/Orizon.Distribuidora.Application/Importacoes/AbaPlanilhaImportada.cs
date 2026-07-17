namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record AbaPlanilhaImportada(
    string Nome,
    int Ordem,
    IReadOnlyList<string> Cabecalhos,
    int QuantidadeLinhas,
    IReadOnlyList<LinhaPlanilhaImportada> Amostra);
