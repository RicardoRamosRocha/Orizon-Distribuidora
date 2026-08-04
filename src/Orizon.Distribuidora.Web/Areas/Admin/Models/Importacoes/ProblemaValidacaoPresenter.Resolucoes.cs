namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;

public static partial class ProblemaValidacaoPresenter
{
    private static string Resolucao(string codigo, string campo, string coluna) => codigo switch
    {
        "IMP_CAMPO_OBRIGATORIO" => $"Preencha a coluna “{coluna}” e valide a planilha novamente.",
        "IMP_CADASTRO_INEXISTENTE" => $"Cadastre o valor ou substitua-o na coluna “{coluna}” por um {NomeCampo(campo).ToLowerInvariant()} ativo.",
        "IMP_CADASTRO_AMBIGUO" => $"Use na coluna “{coluna}” um código, sigla ou documento que identifique somente um cadastro.",
        "IMP_NUMERO_INVALIDO" => $"Informe na coluna “{coluna}” apenas um número, por exemplo 10,50.",
        "IMP_VALOR_NEGATIVO" => $"Informe zero ou um valor positivo na coluna “{coluna}”.",
        "IMP_VALOR_FORA_LIMITE" => $"Reduza o conteúdo da coluna “{coluna}” e valide novamente.",
        "IMP_BOOLEANO_INVALIDO" => $"Use Sim ou Não na coluna “{coluna}”.",
        "IMP_DUPLICIDADE_CODIGO_PLANILHA" => "Mantenha apenas uma linha para cada código de produto.",
        "IMP_DUPLICIDADE_BARRAS_PLANILHA" => "Mantenha apenas uma linha para cada código de barras.",
        "IMP_DUPLICIDADE" => "Remova ou consolide as linhas repetidas e valide novamente.",
        "IMP_CODIGO_BARRAS_EXISTENTE" => "Informe um código de barras livre ou mantenha o código do produto já cadastrado.",
        "IMP_CODIGO_BARRAS_INVALIDO" => "Informe somente dígitos, com no máximo 32 caracteres.",
        "IMP_NCM_INVALIDO" => "Informe o NCM com exatamente oito dígitos.",
        "IMP_PARCEIRO_OBRIGATORIO" => "Informe o CNPJ de um parceiro comercial ativo.",
        "IMP_LOCAL_ESTOQUE_OBRIGATORIO" or "IMP_LOCAL_ESTOQUE_INVALIDO" =>
            "Volte ao mapeamento e selecione um depósito e um local interno válidos.",
        "IMP_RELACIONAMENTO_INCOMPATIVEL" => "Escolha uma subcategoria vinculada à categoria informada.",
        "IMP_PRECO_MENOR_CUSTO" => "Confira custo e venda e ajuste o preço antes de prosseguir.",
        "IMP_PRECO_ZERADO" => "Informe um preço maior que zero ou confirme se o produto pode seguir assim.",
        "IMP_SALDO_INICIAL_EXISTENTE" => "Remova o saldo inicial desta planilha; futuras entradas devem usar a movimentação de estoque.",
        "IMP_TERCEIRO_COM_ESTOQUE" => "Remova o estoque inicial ou altere o tipo para Próprio, se essa for a classificação correta.",
        "IMP_TIPO_CONFLITANTE" => "Mantenha apenas uma definição coerente para o tipo do produto.",
        "IMP_TIPO_INVALIDO" => "Informe Próprio ou Terceiro.",
        _ => $"Corrija a coluna “{coluna}” e valide a planilha novamente."
    };
}
