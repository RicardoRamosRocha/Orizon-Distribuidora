using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;

public enum CategoriaProblemaValidacao
{
    Cadastro, Preenchimento, Formato, Duplicidade, Estoque, Preco, Relacionamento, RegraDeNegocio
}

public sealed record AcaoRapidaValidacaoViewModel(string Rotulo, string ModuloCadastro, bool CriarNovo);

public sealed record ProblemaValidacaoViewModel(
    string Titulo, string Descricao, string Causa, string Resolucao,
    CategoriaProblemaValidacao Categoria, SeveridadeValidacao Severidade,
    int QuantidadeLinhasAfetadas, string AcaoSugerida, string Codigo, string Campo,
    string CampoExibicao, IReadOnlyList<int> LinhasAfetadas,
    IReadOnlyList<AcaoRapidaValidacaoViewModel> AcoesRapidas)
{
    public int QuantidadeProdutosAfetados => QuantidadeLinhasAfetadas;
    public bool Bloqueante => Severidade == SeveridadeValidacao.Erro;
}

public static partial class ProblemaValidacaoPresenter
{
    private static readonly IReadOnlyDictionary<string, string> NomesCampos =
        CatalogoCamposProdutoImportacao.Campos.ToDictionary(x => x.Chave, x => x.Nome, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<ProblemaValidacaoViewModel> Agrupar(
        IEnumerable<ErroValidacaoImportacao> ocorrencias,
        IReadOnlyDictionary<string, string>? mapeamentos = null) => ocorrencias
            .GroupBy(x => new { x.Codigo, x.Campo, x.Severidade })
            .Select(grupo => Criar(grupo.First(), grupo.Select(x => x.NumeroLinha), mapeamentos))
            .OrderBy(x => x.Bloqueante ? 0 : 1)
            .ThenByDescending(x => x.QuantidadeLinhasAfetadas)
            .ThenBy(x => x.Titulo, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public static ProblemaValidacaoViewModel Apresentar(ErroValidacaoImportacao ocorrencia,
        IReadOnlyDictionary<string, string>? mapeamentos = null) => Criar(ocorrencia, [ocorrencia.NumeroLinha], mapeamentos);

    private static ProblemaValidacaoViewModel Criar(ErroValidacaoImportacao ocorrencia, IEnumerable<int> numerosLinha,
        IReadOnlyDictionary<string, string>? mapeamentos)
    {
        var campo = NomeCampo(ocorrencia.Campo);
        var linhas = numerosLinha.Where(x => x > 0).Distinct().Order().ToList();
        var coluna = mapeamentos is not null && mapeamentos.TryGetValue(ocorrencia.Campo, out var mapeada) ? mapeada : campo;
        var resolucao = Resolucao(ocorrencia.Codigo, ocorrencia.Campo, coluna);
        var acoes = AcoesRapidas(ocorrencia.Codigo, ocorrencia.Campo);
        return CriarModelo(ocorrencia, campo, linhas, resolucao, acoes);
    }
}
