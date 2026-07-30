namespace Orizon.Distribuidora.Application.Importacoes;

public static class CatalogoCamposProdutoImportacao
{
    public static IReadOnlyList<CampoImportavel> Campos { get; } =
    [
        Campo("codigo", "Código", "Identificador único do produto.", true, TipoCampoImportacao.Texto, ["codigo", "código", "cod", "sku", "referencia", "referência", "cod produto", "codigo produto"], "Produto.InternalCode"),
        Campo("codigoBarras", "Código de barras", "EAN, GTIN ou código de barras.", false, TipoCampoImportacao.Texto, ["ean", "gtin", "codigo de barras", "código de barras", "cod barras", "barcode"], "Produto.Barcode"),
        Campo("descricao", "Descrição", "Nome principal do produto.", true, TipoCampoImportacao.Texto, ["nome", "produto", "descricao produto"], "Produto.Name"),
        Campo("descricaoComplementar", "Descrição complementar", "Detalhes adicionais.", false, TipoCampoImportacao.Texto, ["complemento", "detalhes"], "Produto.Description"),
        Campo("marca", "Marca", "Código ou nome exato de marca já cadastrada.", false, TipoCampoImportacao.Texto, ["fabricante"], "Produto.BrandId"),
        Campo("categoria", "Categoria", "Código ou nome exato de categoria já cadastrada.", false, TipoCampoImportacao.Texto, ["departamento", "secao"], "Produto.CategoryId"),
        Campo("subcategoria", "Subcategoria", "Código ou nome exato de subcategoria já cadastrada.", false, TipoCampoImportacao.Texto, ["sub categoria"], "Produto.SubcategoryId"),
        Campo("grupo", "Grupo", "Código ou nome exato de grupo já cadastrado.", false, TipoCampoImportacao.Texto, ["grupo produto", "familia"], "Produto.ProductGroupId"),
        Campo("fornecedor", "Fornecedor", "CNPJ/CPF ou nome exato não ambíguo de fornecedor ativo.", false, TipoCampoImportacao.Texto, ["fornecedor principal", "supplier"], "Produto.MainSupplierId"),
        Campo("unidade", "Unidade", "Código, sigla ou nome de unidade já cadastrada.", true, TipoCampoImportacao.Texto, ["unidade", "un", "und", "unidade medida"], "Produto.UnitOfMeasureId"),
        Campo("precoCompra", "Preço Compra", "Custo de aquisição.", false, TipoCampoImportacao.Decimal, ["custo", "preço custo", "preco custo", "valor custo", "valor compra", "pc"], "Produto.CostPrice"),
        Campo("precoVenda", "Preço Venda", "Preço normal de venda.", true, TipoCampoImportacao.Decimal, ["preço venda", "preco venda", "valor venda", "pv", "venda", "preco"], "Produto.SalePrice"),
        Indisponivel("precoPromocional", "Preço Promocional", "O domínio atual não possui preço promocional no cadastro base do produto.", TipoCampoImportacao.Decimal, ["promocao", "preco promo"]),
        Campo("estoqueInicial", "Estoque Inicial", "Entrada inicial auditável no depósito/local selecionado.", false, TipoCampoImportacao.Decimal, ["estoque", "saldo", "quantidade"], "Movimentação de estoque (InitialBalance)"),
        Campo("ncm", "NCM", "Classificação fiscal NCM.", false, TipoCampoImportacao.Texto, ["classificacao fiscal"], "Produto.Ncm"),
        Indisponivel("origem", "Origem", "A origem fiscal ainda não existe no domínio de Produto.", TipoCampoImportacao.Texto, ["origem mercadoria"]),
        Indisponivel("peso", "Peso", "O domínio atual não possui campo de peso no cadastro de Produto.", TipoCampoImportacao.Decimal, ["peso liquido", "kg"]),
        Campo("observacoes", "Observações", "Notas livres.", false, TipoCampoImportacao.Texto, ["obs", "observacao"], "Produto.Notes"),
        Campo("tipoProduto", "Tipo Produto", "Próprio ou Terceiro.", false, TipoCampoImportacao.Texto, ["tipo", "tipo item"], "Produto.ProductType"),
        Campo("status", "Status", "Situação cadastral ativa ou inativa.", false, TipoCampoImportacao.Texto, ["situacao", "ativo"], "Produto.IsActive"),
        Campo("controlaEstoque", "Controla Estoque", "Indica controle de saldo para produto próprio.", false, TipoCampoImportacao.Booleano, ["controlar estoque", "tem estoque"], "Produto.ControlsStock"),
        Campo("produtoProprio", "Produto Próprio", "Atalho para definir o tipo próprio.", false, TipoCampoImportacao.Booleano, ["proprio", "fabricacao propria"], "Produto.ProductType"),
        Campo("produtoTerceiro", "Produto Terceiro", "Atalho para definir o tipo terceiro.", false, TipoCampoImportacao.Booleano, ["terceiro", "revenda"], "Produto.ProductType"),
        Indisponivel("parceiroCodigo", "Código do parceiro", "Parceiro comercial não possui código no domínio atual; nenhuma migration será criada por antecipação.", TipoCampoImportacao.Texto, ["codigo parceiro"]),
        Campo("parceiroCnpj", "CNPJ do parceiro", "CNPJ de parceiro comercial ativo da mesma empresa.", false, TipoCampoImportacao.Texto, ["cnpj parceiro", "documento parceiro"], "Produto.PartnerId")
    ];

    private static CampoImportavel Campo(string chave, string nome, string descricao, bool obrigatorio, TipoCampoImportacao tipo, string[] sinonimos, string destino) =>
        new(chave, nome, descricao, obrigatorio, tipo, true, true, sinonimos, destino);

    private static CampoImportavel Indisponivel(string chave, string nome, string motivo, TipoCampoImportacao tipo, string[] sinonimos) =>
        new(chave, nome, motivo, false, tipo, false, false, sinonimos, "Não persistido", motivo);
}
