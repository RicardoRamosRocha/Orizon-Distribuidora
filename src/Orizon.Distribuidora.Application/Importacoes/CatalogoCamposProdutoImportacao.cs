namespace Orizon.Distribuidora.Application.Importacoes;

public static class CatalogoCamposProdutoImportacao
{
    public static IReadOnlyList<CampoImportavel> Campos { get; } =
    [
        Campo("codigo", "Código", "Identificador único do produto.", true, TipoCampoImportacao.Texto, ["cod", "cod produto", "codigo produto", "sku", "referencia"]),
        Campo("codigoBarras", "Código de barras", "EAN, GTIN ou código de barras.", false, TipoCampoImportacao.Texto, ["ean", "gtin", "cod barras", "barcode"]),
        Campo("descricao", "Descrição", "Nome principal do produto.", true, TipoCampoImportacao.Texto, ["nome", "produto", "descricao produto"]),
        Campo("descricaoComplementar", "Descrição complementar", "Detalhes adicionais.", false, TipoCampoImportacao.Texto, ["complemento", "detalhes"]),
        Campo("marca", "Marca", "Marca ou fabricante.", false, TipoCampoImportacao.Texto, ["fabricante"]),
        Campo("categoria", "Categoria", "Categoria comercial.", false, TipoCampoImportacao.Texto, ["departamento", "secao"]),
        Campo("subcategoria", "Subcategoria", "Subcategoria comercial.", false, TipoCampoImportacao.Texto, ["sub categoria"]),
        Campo("grupo", "Grupo", "Grupo de produtos.", false, TipoCampoImportacao.Texto, ["grupo produto", "familia"]),
        Campo("fornecedor", "Fornecedor", "Fornecedor principal.", false, TipoCampoImportacao.Texto, ["fornecedor principal", "supplier"]),
        Campo("unidade", "Unidade", "Unidade de medida.", true, TipoCampoImportacao.Texto, ["un", "und", "unidade medida"]),
        Campo("precoCompra", "Preço Compra", "Custo de aquisição.", false, TipoCampoImportacao.Decimal, ["custo", "preco custo", "valor compra", "pc"]),
        Campo("precoVenda", "Preço Venda", "Preço normal de venda.", true, TipoCampoImportacao.Decimal, ["pv", "venda", "preco", "valor venda"]),
        Campo("precoPromocional", "Preço Promocional", "Preço promocional.", false, TipoCampoImportacao.Decimal, ["promocao", "preco promo"]),
        Campo("estoqueInicial", "Estoque Inicial", "Saldo inicial.", false, TipoCampoImportacao.Decimal, ["estoque", "saldo", "quantidade"]),
        Campo("ncm", "NCM", "Classificação fiscal NCM.", false, TipoCampoImportacao.Texto, ["classificacao fiscal"]),
        Campo("origem", "Origem", "Origem fiscal da mercadoria.", false, TipoCampoImportacao.Texto, ["origem mercadoria"]),
        Campo("peso", "Peso", "Peso do produto.", false, TipoCampoImportacao.Decimal, ["peso liquido", "kg"]),
        Campo("observacoes", "Observações", "Notas livres.", false, TipoCampoImportacao.Texto, ["obs", "observacao"]),
        Campo("tipoProduto", "Tipo Produto", "Tipo do produto.", false, TipoCampoImportacao.Texto, ["tipo", "tipo item"]),
        Campo("status", "Status", "Situação cadastral.", false, TipoCampoImportacao.Texto, ["situacao", "ativo"]),
        Campo("controlaEstoque", "Controla Estoque", "Indica controle de saldo.", false, TipoCampoImportacao.Booleano, ["controlar estoque", "tem estoque"]),
        Campo("produtoProprio", "Produto Próprio", "Indica fabricação própria.", false, TipoCampoImportacao.Booleano, ["proprio", "fabricacao propria"]),
        Campo("produtoTerceiro", "Produto Terceiro", "Indica produto de terceiros.", false, TipoCampoImportacao.Booleano, ["terceiro", "revenda"])
    ];

    private static CampoImportavel Campo(string chave, string nome, string descricao, bool obrigatorio, TipoCampoImportacao tipo, string[] sinonimos) =>
        new(chave, nome, descricao, obrigatorio, tipo, true, true, sinonimos);
}
