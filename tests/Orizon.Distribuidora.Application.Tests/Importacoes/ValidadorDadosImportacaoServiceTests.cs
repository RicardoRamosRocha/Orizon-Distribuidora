using System.Reflection;
using System.Text.Json;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ValidadorDadosImportacaoServiceTests
{
    private readonly ValidadorDadosImportacaoService service = new();
    private static readonly MapeamentoColunasImportacao Mapping = new(new Dictionary<string,string>{{"codigo","Código"},{"descricao","Descrição"},{"descricaoComplementar","Complemento"},{"unidade","Un"},{"precoCompra","Custo"},{"precoVenda","Venda"},{"estoqueInicial","Estoque"},{"tipoProduto","Tipo"},{"codigoBarras","Barras"},{"marca","Marca"},{"categoria","Categoria"},{"subcategoria","Subcategoria"},{"grupo","Grupo"},{"fornecedor","Fornecedor"},{"parceiroCnpj","Parceiro"},{"ncm","NCM"},{"observacoes","Observações"},{"status","Status"},{"controlaEstoque","ControlaEstoque"}});

    [Fact] public async Task Linha_valida_e_novo_produto(){var r=await Validate(Row(2,"A1","Produto"));Assert.True(r.Linhas[0].PodeImportar);Assert.Equal(TipoOperacaoImportacao.Inserir,r.Linhas[0].Operacao);}
    [Fact] public async Task Codigo_obrigatorio(){var r=await Validate(Row(2," ","Produto"));AssertCode(r,"IMP_CAMPO_OBRIGATORIO");}
    [Fact] public async Task Descricao_obrigatoria(){var r=await Validate(Row(2,"A1"," "));AssertCode(r,"IMP_CAMPO_OBRIGATORIO");}
    [Fact] public async Task Codigo_duplicado_marca_todas(){var r=await Validate(Row(2,"a1","Um"),Row(3," A1 ","Dois"));Assert.Equal(2,r.QuantidadeDuplicidades);Assert.All(r.Linhas,x=>Assert.Equal(StatusValidacaoLinha.Duplicada,x.Status));}
    [Fact] public async Task Decimal_brasileiro(){var r=await Validate(Row(2,"A1","Produto",custo:"1.234,56"));Assert.Equal(1234.56m,r.Linhas[0].ValoresConvertidos["precoCompra"]);}
    [Fact] public async Task Decimal_internacional(){var r=await Validate(Row(2,"A1","Produto",venda:"1234.56"));Assert.Equal(1234.56m,r.Linhas[0].ValoresConvertidos["precoVenda"]);}
    [Fact] public async Task Numero_invalido(){var r=await Validate(Row(2,"A1","Produto",venda:"abc"));AssertCode(r,"IMP_NUMERO_INVALIDO");}
    [Fact] public async Task Valor_negativo(){var r=await Validate(Row(2,"A1","Produto",venda:"-1"));AssertCode(r,"IMP_VALOR_NEGATIVO");}
    [Fact] public async Task Estoque_inicial_negativo(){var r=await Validate(Row(2,"A1","Produto",estoque:"-1"));Assert.Contains(r.Linhas[0].Erros,x=>x.Campo=="estoqueInicial"&&x.Codigo=="IMP_VALOR_NEGATIVO");}
    [Fact] public async Task Terceiro_com_estoque(){var r=await Validate(Row(2,"A1","Produto",estoque:"2",tipo:"terceiro"));AssertCode(r,"IMP_TERCEIRO_COM_ESTOQUE");}
    [Fact] public async Task Produto_proprio_valido(){var r=await Validate(Row(2,"A1","Produto",estoque:"2",tipo:"próprio"));Assert.True(r.Linhas[0].PodeImportar);}
    [Fact] public async Task Unidade_inexistente(){var r=await Validate(Row(2,"A1","Produto",unidade:"CX"));AssertCode(r,"IMP_CADASTRO_INEXISTENTE");}
    [Fact] public async Task Codigo_barras_preserva_zeros(){var r=await Validate(Row(2,"A1","Produto",barras:"001234"));Assert.Equal("001234",r.Linhas[0].ValoresConvertidos["codigoBarras"]);}
    [Fact] public async Task Linha_vazia_ignorada(){var r=await Validate(new LinhaPlanilhaImportada(2,new Dictionary<string,string?>()));Assert.Equal(1,r.QuantidadeIgnoradas);var linha=Assert.Single(r.Linhas);Assert.Equal(StatusValidacaoLinha.Ignorada,linha.Status);Assert.Equal(TipoOperacaoImportacao.Ignorar,linha.Operacao);}
    [Fact] public async Task Produto_existente_com_alteracao(){var existing=Product("A1","Antigo");var r=await Validate([Row(2,"A1","Novo")],[existing]);Assert.Equal(TipoOperacaoImportacao.Atualizar,r.Linhas[0].Operacao);Assert.NotEmpty(r.Linhas[0].Alteracoes);}
    [Fact] public async Task Produto_existente_sem_alteracao_ignorado(){var existing=Product("A1","Produto");var r=await Validate([Row(2,"A1","Produto")],[existing]);Assert.Equal(TipoOperacaoImportacao.Ignorar,r.Linhas[0].Operacao);}
    [Fact]
    public async Task Produto_ignorado_com_aviso_mantem_classificacao_ignorada()
    {
        var existing = new ProdutoExistenteImportacao(
            Guid.NewGuid(), "A1", "Produto", null, 0, 0,
            Guid.Parse("11111111-1111-1111-1111-111111111111"), true);
        var result = await Validate([Row(2, "A1", "Produto", venda: "0")], [existing]);

        Assert.Equal(TipoOperacaoImportacao.Ignorar, result.Linhas[0].Operacao);
        Assert.Equal(StatusValidacaoLinha.Ignorada, result.Linhas[0].Status);
        Assert.NotEmpty(result.Linhas[0].Avisos);
    }
    [Fact] public async Task Produto_existente_com_alteracoes_em_campos_opcionais_e_atualizavel()
    {
        var category=Guid.NewGuid();var subcategory=Guid.NewGuid();var brand=Guid.NewGuid();var group=Guid.NewGuid();var supplier=Guid.NewGuid();var partner=Guid.NewGuid();
        var refs=References(marcas:[new(brand,"MAR","Marca X")],parceiros:[new(partner,null,"Parceiro","12345678000195")],categorias:[new(category,"CAT","Categoria X")],subcategorias:[new(subcategory,"SUB","Subcategoria X",ParentId:category)],grupos:[new(group,"GRP","Grupo X")],fornecedores:[new(supplier,"FOR","Fornecedor X")]);
        var row=Row(2,"A1","Produto",complemento:"Detalhes",marca:"MAR",categoria:"CAT",subcategoria:"SUB",grupo:"GRP",fornecedor:"Fornecedor X",parceiro:"12345678000195",ncm:"12345678",observacoes:"Observação",status:"não");
        var r=await ValidateWithRefs(row,refs,[Product("A1","Produto")]);
        Assert.Empty(r.Linhas[0].Erros);
        Assert.Equal(TipoOperacaoImportacao.Atualizar,r.Linhas[0].Operacao);
        Assert.Subset(new HashSet<string>{"Descrição complementar","Categoria","Subcategoria","Marca","Grupo","Fornecedor","Parceiro","NCM","Status","Observações"},r.Linhas[0].Alteracoes.Select(x=>x.Campo).ToHashSet());
    }
    [Fact] public async Task Estoque_inicial_que_altera_deposito_padrao_marca_atualizacao(){var r=await Validate([Row(2,"A1","Produto",estoque:"2")],[Product("A1","Produto")]);Assert.Equal(TipoOperacaoImportacao.Atualizar,r.Linhas[0].Operacao);Assert.Contains(r.Linhas[0].Alteracoes,x=>x.Campo=="Depósito padrão");Assert.Contains(r.Linhas[0].Alteracoes,x=>x.Campo=="Local interno padrão");}
    [Fact]
    public async Task Estoque_inicial_existente_no_deposito_bloqueia_linha_na_validacao()
    {
        var existing = Product("A1", "Produto");
        var result = await Validate(
            [Row(2, "A1", "Produto", estoque: "2")],
            [existing],
            produtosComSaldoInicialNoDeposito: new HashSet<Guid> { existing.Id });

        var error = Assert.Single(result.Linhas[0].Erros, item => item.Codigo == "IMP_SALDO_INICIAL_EXISTENTE");
        Assert.Equal("estoqueInicial", error.Campo);
        Assert.Equal("O produto já possui saldo inicial no depósito selecionado.", error.Mensagem);
        Assert.Equal(TipoOperacaoImportacao.Bloquear, result.Linhas[0].Operacao);
        Assert.False(result.Linhas[0].PodeImportar);
    }
    [Fact] public async Task Importacao_parcial_permite_validas(){var r=await Validate(Row(2,"A1","Ok"),Row(3,"","Erro"));Assert.True(r.PodeImportar);Assert.Equal(1,r.QuantidadeValida);}
    [Fact] public async Task Unidade_vazia_e_preenchida_com_un_e_contabilizada(){var r=await Validate(Row(2,"A1","Produto",unidade:"",venda:"10"));Assert.Equal(1,r.QuantidadeUnidadesPreenchidasAutomaticamente);Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"),r.Linhas[0].ValoresConvertidos["unidadeId"]);Assert.DoesNotContain(r.Linhas[0].Erros,x=>x.Campo=="unidade");}
    [Fact]
    public async Task Atualizacao_parcial_com_unidade_vazia_preserva_unidade_existente()
    {
        var currentUnitId = Guid.NewGuid();
        var existing = new ProdutoExistenteImportacao(Guid.NewGuid(), "A1", "Produto", null, 0, 10, currentUnitId, true);
        var result = await Validate([Row(2, "A1", "Produto", unidade: "")], [existing]);

        Assert.Equal(0, result.QuantidadeUnidadesPreenchidasAutomaticamente);
        Assert.False(result.Linhas[0].ValoresConvertidos.ContainsKey("unidadeId"));
        Assert.DoesNotContain(result.Linhas[0].Erros, error => error.Campo == "unidade");
        Assert.DoesNotContain(result.Linhas[0].Alteracoes, change => change.Campo == "Unidade");
        Assert.Equal(TipoOperacaoImportacao.Ignorar, result.Linhas[0].Operacao);
    }
    [Fact] public async Task Preco_venda_continua_obrigatorio(){var r=await Validate(Row(2,"A1","Produto",unidade:"",venda:null));Assert.Single(r.Linhas[0].Erros,x=>x.Codigo=="IMP_CAMPO_OBRIGATORIO");}
    [Fact] public async Task Campos_texto_sao_normalizados_antes_da_validacao(){var r=await Validate(Row(2," A1 "," Produto ",unidade:" UN "));Assert.Equal("A1",r.Linhas[0].CodigoProduto);Assert.Equal("Produto",r.Linhas[0].Descricao);Assert.Null(r.Linhas[0].DadosOriginais["Custo"]);}
    [Fact] public async Task Relacionamento_ambiguo_bloqueia_linha(){var refs=References(marcas:[new(Guid.NewGuid(),null,"Marca X"),new(Guid.NewGuid(),null,"marca x")]);var r=await ValidateWithRefs(Row(2,"A1","Produto",marca:"Marca X"),refs);AssertCode(r,"IMP_CADASTRO_AMBIGUO");}
    [Fact] public async Task Parceiro_terceiro_resolvido_por_cnpj(){var partner=Guid.NewGuid();var refs=References(parceiros:[new(partner,null,"Parceiro","12345678000195")]);var r=await ValidateWithRefs(Row(2,"A1","Produto",tipo:"terceiro",parceiro:"12.345.678/0001-95"),refs);Assert.Equal(partner,r.Linhas[0].ValoresConvertidos["parceiroId"]);Assert.DoesNotContain(r.Linhas[0].Erros,x=>x.Codigo=="IMP_PARCEIRO_OBRIGATORIO");}
    [Fact] public async Task Produto_terceiro_sem_parceiro_e_bloqueado(){var r=await Validate(Row(2,"A1","Produto",tipo:"terceiro"));AssertCode(r,"IMP_PARCEIRO_OBRIGATORIO");}

    [Fact]
    public async Task Atualizacao_completa_mantem_vazios_mapeados_como_null_e_limpa_campos_opcionais()
    {
        var categoryId = Guid.NewGuid();
        var existing = new ProdutoExistenteImportacao(
            Guid.NewGuid(), "A1", "Produto", "001234", 0, 10,
            Guid.Parse("11111111-1111-1111-1111-111111111111"), true,
            DescricaoComplementar: "Complemento", CategoriaId: categoryId);
        var result = await Validate([Row(2, "A1", "Produto")], [existing], ignorarVaziosAtualizacao: false);
        var converted = result.Linhas[0].ValoresConvertidos;

        Assert.True(converted.ContainsKey("codigoBarras"));
        Assert.True(converted.ContainsKey("descricaoComplementar"));
        Assert.True(converted.ContainsKey("categoriaId"));
        Assert.Null(converted["codigoBarras"]);
        Assert.Null(converted["descricaoComplementar"]);
        Assert.Null(converted["categoriaId"]);
        Assert.Contains(result.Linhas[0].Alteracoes, change => change.Campo == "Código de barras" && change.ValorConvertido is null);
        Assert.Contains(result.Linhas[0].Alteracoes, change => change.Campo == "Descrição complementar" && change.ValorConvertido is null);
        Assert.Contains(result.Linhas[0].Alteracoes, change => change.Campo == "Categoria" && change.ValorConvertido is null);

        var product = new Product(existing.Id, "A1", "Produto", existing.UnidadeId, ProductType.Own);
        product.Update(
            product.InternalCode, null, existing.CodigoBarras, null, product.Name, null,
            existing.DescricaoComplementar, ProductType.Own, true, true, existing.CategoriaId,
            null, null, existing.UnidadeId, null, null, null, null, null, null, null,
            0, 10, null, null, null, null, null);
        var normalized = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(converted))!;
        var apply = typeof(ExecutorImportacaoProdutosService).GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Static)!;
        apply.Invoke(null, [product, normalized, new OpcoesValidacaoImportacao(IgnorarVaziosAtualizacao: false), false]);

        Assert.Null(product.Barcode);
        Assert.Null(product.Description);
        Assert.Null(product.CategoryId);
    }

    [Fact]
    public async Task Atualizacao_parcial_continua_omitindo_campos_mapeados_vazios()
    {
        var existing = new ProdutoExistenteImportacao(
            Guid.NewGuid(), "A1", "Produto", "001234", 0, 10,
            Guid.Parse("11111111-1111-1111-1111-111111111111"), true,
            DescricaoComplementar: "Complemento", CategoriaId: Guid.NewGuid());
        var result = await Validate([Row(2, "A1", "Produto")], [existing]);
        var converted = result.Linhas[0].ValoresConvertidos;

        Assert.False(converted.ContainsKey("codigoBarras"));
        Assert.False(converted.ContainsKey("descricaoComplementar"));
        Assert.False(converted.ContainsKey("categoriaId"));
        Assert.Equal(TipoOperacaoImportacao.Ignorar, result.Linhas[0].Operacao);
    }

    [Fact]
    public async Task Atualizacao_completa_nao_inclui_campo_nao_mapeado()
    {
        var columns = Mapping.Colunas
            .Where(item => item.Key is not ("descricaoComplementar" or "categoria"))
            .ToDictionary(item => item.Key, item => item.Value);
        var mapping = new MapeamentoColunasImportacao(columns);
        var existing = new ProdutoExistenteImportacao(
            Guid.NewGuid(), "A1", "Produto", null, 0, 10,
            Guid.Parse("11111111-1111-1111-1111-111111111111"), true,
            DescricaoComplementar: "Complemento", CategoriaId: Guid.NewGuid());
        var result = await Validate([Row(2, "A1", "Produto")], [existing], false, mapping);
        var converted = result.Linhas[0].ValoresConvertidos;

        Assert.False(converted.ContainsKey("descricaoComplementar"));
        Assert.False(converted.ContainsKey("categoriaId"));
    }

    private Task<ResultadoValidacaoImportacao> Validate(params LinhaPlanilhaImportada[] rows)=>Validate(rows,[]);
    private Task<ResultadoValidacaoImportacao> Validate(
        IReadOnlyList<LinhaPlanilhaImportada> rows,
        IReadOnlyList<ProdutoExistenteImportacao> existing,
        bool ignorarVaziosAtualizacao = true,
        MapeamentoColunasImportacao? mapping = null,
        IReadOnlySet<Guid>? produtosComSaldoInicialNoDeposito = null)
    {
        mapping ??= Mapping;
        var warehouse = Guid.NewGuid(); var location = Guid.NewGuid();
        var references = References();
        var existingCodes = existing.Select(item => ValidadorDadosImportacaoService.NormalizarCodigo(item.Codigo)).ToHashSet(StringComparer.Ordinal);
        var normalizacao = NormalizadorDadosImportacao.Normalizar(rows, mapping, row =>
            !ignorarVaziosAtualizacao ||
            !existingCodes.Contains(ValidadorDadosImportacaoService.NormalizarCodigo(
                mapping.Colunas.TryGetValue("codigo", out var column) && row.Valores.TryGetValue(column, out var code) ? code : null)));
        return service.ValidarAsync(new(Guid.NewGuid(),Guid.NewGuid(),null,normalizacao.Linhas,mapping,
            new(IgnorarVaziosAtualizacao: ignorarVaziosAtualizacao, DepositoId: warehouse, LocalInternoId: location),existing,references,
            normalizacao.QuantidadeUnidadesPreenchidasAutomaticamente, produtosComSaldoInicialNoDeposito));
    }
    private Task<ResultadoValidacaoImportacao> ValidateWithRefs(LinhaPlanilhaImportada row,ReferenciasProdutoImportacao refs,IReadOnlyList<ProdutoExistenteImportacao>? existing=null){var normalizacao=NormalizadorDadosImportacao.Normalizar([row],Mapping);return service.ValidarAsync(new(Guid.NewGuid(),Guid.NewGuid(),null,normalizacao.Linhas,Mapping,new(DepositoId:Guid.NewGuid(),LocalInternoId:Guid.NewGuid()),existing??[],refs,normalizacao.QuantidadeUnidadesPreenchidasAutomaticamente));}
    private static ReferenciasProdutoImportacao References(IReadOnlyList<ReferenciaImportacao>? marcas=null,IReadOnlyList<ReferenciaImportacao>? parceiros=null,IReadOnlyList<ReferenciaImportacao>? categorias=null,IReadOnlyList<ReferenciaImportacao>? subcategorias=null,IReadOnlyList<ReferenciaImportacao>? grupos=null,IReadOnlyList<ReferenciaImportacao>? fornecedores=null)=>new([new(Guid.Parse("11111111-1111-1111-1111-111111111111"),"UN","Unidade")],marcas??[],categorias??[],subcategorias??[],grupos??[],fornecedores??[],parceiros??[],true,true);
    private static LinhaPlanilhaImportada Row(int line,string code,string name,string unidade="UN",string? custo=null,string? venda="10",string? estoque=null,string? tipo="próprio",string? barras=null,string? marca=null,string? parceiro=null,string? complemento=null,string? categoria=null,string? subcategoria=null,string? grupo=null,string? fornecedor=null,string? ncm=null,string? observacoes=null,string? status=null,string? controlaEstoque=null)=>new(line,new Dictionary<string,string?>{{"Código",code},{"Descrição",name},{"Complemento",complemento},{"Un",unidade},{"Custo",custo},{"Venda",venda},{"Estoque",estoque},{"Tipo",tipo},{"Barras",barras},{"Marca",marca},{"Categoria",categoria},{"Subcategoria",subcategoria},{"Grupo",grupo},{"Fornecedor",fornecedor},{"Parceiro",parceiro},{"NCM",ncm},{"Observações",observacoes},{"Status",status},{"ControlaEstoque",controlaEstoque}});
    private static ProdutoExistenteImportacao Product(string code,string name)=>new(Guid.NewGuid(),code,name,null,0,10,Guid.Parse("11111111-1111-1111-1111-111111111111"),true);
    private static void AssertCode(ResultadoValidacaoImportacao r,string code)=>Assert.Contains(r.Linhas.SelectMany(x=>x.Erros),x=>x.Codigo==code);
}
