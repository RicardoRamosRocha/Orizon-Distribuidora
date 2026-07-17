namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record PlanilhaImportada(
    string? AbaSelecionada,
    IReadOnlyList<AbaPlanilhaImportada> Abas)
{
    public AbaPlanilhaImportada? AbaAtual =>
        Abas.FirstOrDefault(aba => aba.Nome == AbaSelecionada) ?? Abas.FirstOrDefault();

    public IReadOnlyList<string> Cabecalhos => AbaAtual?.Cabecalhos ?? [];

    public IReadOnlyList<LinhaPlanilhaImportada> Linhas => AbaAtual?.Amostra ?? [];
}
