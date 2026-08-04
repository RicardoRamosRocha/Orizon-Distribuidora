namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;

public static partial class ProblemaValidacaoPresenter
{
    private static CategoriaProblemaValidacao Categoria(string codigo) => codigo switch
    {
        "IMP_CADASTRO_INEXISTENTE" or "IMP_CADASTRO_AMBIGUO" => CategoriaProblemaValidacao.Cadastro,
        "IMP_CAMPO_OBRIGATORIO" => CategoriaProblemaValidacao.Preenchimento,
        "IMP_NUMERO_INVALIDO" or "IMP_VALOR_FORA_LIMITE" or "IMP_BOOLEANO_INVALIDO" or
            "IMP_CODIGO_BARRAS_INVALIDO" or "IMP_NCM_INVALIDO" or "IMP_TIPO_INVALIDO" => CategoriaProblemaValidacao.Formato,
        "IMP_DUPLICIDADE" or "IMP_DUPLICIDADE_CODIGO_PLANILHA" or "IMP_DUPLICIDADE_BARRAS_PLANILHA" or
            "IMP_CODIGO_BARRAS_EXISTENTE" => CategoriaProblemaValidacao.Duplicidade,
        "IMP_LOCAL_ESTOQUE_OBRIGATORIO" or "IMP_LOCAL_ESTOQUE_INVALIDO" or "IMP_SALDO_INICIAL_EXISTENTE" or
            "IMP_TERCEIRO_COM_ESTOQUE" => CategoriaProblemaValidacao.Estoque,
        "IMP_PRECO_MENOR_CUSTO" or "IMP_PRECO_ZERADO" => CategoriaProblemaValidacao.Preco,
        "IMP_RELACIONAMENTO_INCOMPATIVEL" => CategoriaProblemaValidacao.Relacionamento,
        _ => CategoriaProblemaValidacao.RegraDeNegocio
    };

    private static IReadOnlyList<AcaoRapidaValidacaoViewModel> AcoesRapidas(string codigo, string campo)
    {
        if (codigo is not ("IMP_CADASTRO_INEXISTENTE" or "IMP_CADASTRO_AMBIGUO")) return [];

        var cadastro = campo switch
        {
            "categoria" => (Singular: "Categoria", Modulo: "categorias"),
            "marca" => (Singular: "Marca", Modulo: "marcas"),
            "fornecedor" => (Singular: "Fornecedor", Modulo: "fornecedores"),
            "unidade" => (Singular: "Unidade", Modulo: "unidades-medida"),
            _ => default
        };
        if (string.IsNullOrEmpty(cadastro.Modulo)) return [];

        var acoes = new List<AcaoRapidaValidacaoViewModel>();
        if (codigo == "IMP_CADASTRO_INEXISTENTE")
        {
            acoes.Add(new($"Criar {cadastro.Singular}", cadastro.Modulo, true));
        }
        acoes.Add(new("Escolher existente", cadastro.Modulo, false));
        return acoes;
    }
}
