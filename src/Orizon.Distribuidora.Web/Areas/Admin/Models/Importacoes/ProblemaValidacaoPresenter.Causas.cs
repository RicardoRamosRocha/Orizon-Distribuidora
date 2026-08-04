namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;

public static partial class ProblemaValidacaoPresenter
{
    private static string Causa(string codigo, string campo) => codigo switch
    {
        "IMP_CAMPO_OBRIGATORIO" => $"A planilha não trouxe {campo.ToLowerInvariant()} em todas as linhas obrigatórias.",
        "IMP_CADASTRO_INEXISTENTE" => $"O valor informado não corresponde a nenhum cadastro ativo de {campo.ToLowerInvariant()}.",
        "IMP_CADASTRO_AMBIGUO" => $"O valor informado identifica mais de um cadastro de {campo.ToLowerInvariant()}.",
        "IMP_NUMERO_INVALIDO" => "O conteúdo não pôde ser interpretado como número.",
        "IMP_VALOR_NEGATIVO" => "A regra do produto aceita somente zero ou valores positivos.",
        "IMP_VALOR_FORA_LIMITE" => "O conteúdo é maior que o tamanho aceito pelo cadastro do produto.",
        "IMP_BOOLEANO_INVALIDO" => "A opção informada não representa sim ou não.",
        "IMP_DUPLICIDADE_CODIGO_PLANILHA" or "IMP_DUPLICIDADE_BARRAS_PLANILHA" or "IMP_DUPLICIDADE" =>
            "A mesma identificação aparece em mais de uma linha da planilha.",
        "IMP_CODIGO_BARRAS_EXISTENTE" => "Outro produto cadastrado já utiliza esse código de barras.",
        "IMP_CODIGO_BARRAS_INVALIDO" => "O código de barras possui letras, símbolos ou mais dígitos que o permitido.",
        "IMP_NCM_INVALIDO" => "O NCM não possui os oito dígitos esperados.",
        "IMP_PARCEIRO_OBRIGATORIO" => "Produtos de terceiros precisam estar ligados a um parceiro comercial ativo.",
        "IMP_LOCAL_ESTOQUE_OBRIGATORIO" => "Há saldo inicial na planilha, mas depósito e local interno não foram selecionados.",
        "IMP_LOCAL_ESTOQUE_INVALIDO" => "O local interno selecionado não pertence ao depósito ou não está ativo.",
        "IMP_RELACIONAMENTO_INCOMPATIVEL" => "A subcategoria informada pertence a outra categoria.",
        "IMP_PRECO_MENOR_CUSTO" => "O preço de venda informado é menor que o custo do produto.",
        "IMP_PRECO_ZERADO" => "Um preço necessário para o produto foi informado como zero.",
        "IMP_SALDO_INICIAL_EXISTENTE" => "O produto já recebeu saldo inicial no depósito selecionado.",
        "IMP_TERCEIRO_COM_ESTOQUE" => "Somente produtos próprios podem controlar estoque físico.",
        "IMP_TIPO_CONFLITANTE" => "As colunas que definem o tipo do produto apontam para opções diferentes.",
        "IMP_TIPO_INVALIDO" => "O tipo informado não corresponde a Próprio ou Terceiro.",
        _ => "Os dados da planilha não atendem ao formato esperado para o produto."
    };
}
