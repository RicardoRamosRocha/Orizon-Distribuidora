using Orizon.Distribuidora.Application.Importacoes;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;

public static partial class ProblemaValidacaoPresenter
{
    private static ProblemaValidacaoViewModel CriarModelo(ErroValidacaoImportacao ocorrencia, string campo,
        IReadOnlyList<int> linhas, string resolucao, IReadOnlyList<AcaoRapidaValidacaoViewModel> acoes) => new(
        Titulo(ocorrencia.Codigo, campo),
        $"{Quantidade(linhas.Count)} com problema em {campo.ToLowerInvariant()}.",
        Causa(ocorrencia.Codigo, campo), resolucao, Categoria(ocorrencia.Codigo), ocorrencia.Severidade, linhas.Count,
        acoes.Count > 0 ? $"{resolucao} Use uma das ações abaixo para resolver agora." : resolucao,
        ocorrencia.Codigo, ocorrencia.Campo, campo, linhas, acoes);

    private static string NomeCampo(string campo) =>
        NomesCampos.TryGetValue(campo, out var nome) ? nome : "Dados do produto";

    private static string Quantidade(int quantidade) =>
        quantidade == 1 ? "1 produto foi afetado" : $"{quantidade} produtos foram afetados";

    private static string Titulo(string codigo, string campo) => codigo switch
    {
        "IMP_CAMPO_OBRIGATORIO" => $"{campo} não foi informado",
        "IMP_CADASTRO_INEXISTENTE" => $"{campo} não existe no cadastro",
        "IMP_CADASTRO_AMBIGUO" => $"{campo} corresponde a mais de um cadastro",
        "IMP_NUMERO_INVALIDO" => $"{campo} contém um número inválido",
        "IMP_VALOR_NEGATIVO" => $"{campo} não pode ser negativo",
        "IMP_VALOR_FORA_LIMITE" => $"{campo} ultrapassa o limite permitido",
        "IMP_BOOLEANO_INVALIDO" => $"{campo} contém uma opção inválida",
        "IMP_DUPLICIDADE_CODIGO_PLANILHA" => "Código de produto repetido na planilha",
        "IMP_DUPLICIDADE_BARRAS_PLANILHA" => "Código de barras repetido na planilha",
        "IMP_DUPLICIDADE" => "Produto repetido na planilha",
        "IMP_CODIGO_BARRAS_EXISTENTE" => "Código de barras já está em uso",
        "IMP_CODIGO_BARRAS_INVALIDO" => "Código de barras em formato inválido",
        "IMP_NCM_INVALIDO" => "NCM em formato inválido",
        "IMP_PARCEIRO_OBRIGATORIO" => "Parceiro comercial não foi informado",
        "IMP_LOCAL_ESTOQUE_OBRIGATORIO" => "Local do estoque não foi selecionado",
        "IMP_LOCAL_ESTOQUE_INVALIDO" => "Local do estoque não está disponível",
        "IMP_RELACIONAMENTO_INCOMPATIVEL" => "Categoria e subcategoria não combinam",
        "IMP_PRECO_MENOR_CUSTO" => "Preço de venda está abaixo do custo",
        "IMP_PRECO_ZERADO" => "Preço do produto está zerado",
        "IMP_SALDO_INICIAL_EXISTENTE" => "Produto já possui saldo inicial",
        "IMP_TERCEIRO_COM_ESTOQUE" => "Produto de terceiro não pode receber estoque",
        "IMP_TIPO_CONFLITANTE" => "Tipo do produto possui informações conflitantes",
        "IMP_TIPO_INVALIDO" => "Tipo do produto não foi reconhecido",
        _ => $"Revise {campo.ToLowerInvariant()}"
    };
}
