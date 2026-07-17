using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ValidadorDadosImportacaoServiceTests
{
    private readonly ValidadorDadosImportacaoService service = new();
    private static readonly MapeamentoColunasImportacao Mapping = new(new Dictionary<string,string>{{"codigo","Código"},{"descricao","Descrição"},{"unidade","Un"},{"precoCompra","Custo"},{"precoVenda","Venda"},{"estoqueInicial","Estoque"},{"tipoProduto","Tipo"},{"codigoBarras","Barras"}});

    [Fact] public async Task Linha_valida_e_novo_produto(){var r=await Validate(Row(2,"A1","Produto"));Assert.True(r.Linhas[0].PodeImportar);Assert.Equal(TipoOperacaoImportacao.Inserir,r.Linhas[0].Operacao);}
    [Fact] public async Task Codigo_obrigatorio(){var r=await Validate(Row(2," ","Produto"));AssertCode(r,"IMP_CAMPO_OBRIGATORIO");}
    [Fact] public async Task Descricao_obrigatoria(){var r=await Validate(Row(2,"A1"," "));AssertCode(r,"IMP_CAMPO_OBRIGATORIO");}
    [Fact] public async Task Codigo_duplicado_marca_todas(){var r=await Validate(Row(2,"a1","Um"),Row(3," A1 ","Dois"));Assert.Equal(2,r.QuantidadeDuplicidades);Assert.All(r.Linhas,x=>Assert.Equal(StatusValidacaoLinha.Duplicada,x.Status));}
    [Fact] public async Task Decimal_brasileiro(){var r=await Validate(Row(2,"A1","Produto",custo:"1.234,56"));Assert.Equal(1234.56m,r.Linhas[0].ValoresConvertidos["precoCompra"]);}
    [Fact] public async Task Decimal_internacional(){var r=await Validate(Row(2,"A1","Produto",venda:"1234.56"));Assert.Equal(1234.56m,r.Linhas[0].ValoresConvertidos["precoVenda"]);}
    [Fact] public async Task Numero_invalido(){var r=await Validate(Row(2,"A1","Produto",venda:"abc"));AssertCode(r,"IMP_NUMERO_INVALIDO");}
    [Fact] public async Task Valor_negativo(){var r=await Validate(Row(2,"A1","Produto",venda:"-1"));AssertCode(r,"IMP_VALOR_NEGATIVO");}
    [Fact] public async Task Terceiro_com_estoque(){var r=await Validate(Row(2,"A1","Produto",estoque:"2",tipo:"terceiro"));AssertCode(r,"IMP_TERCEIRO_COM_ESTOQUE");}
    [Fact] public async Task Produto_proprio_valido(){var r=await Validate(Row(2,"A1","Produto",estoque:"2",tipo:"próprio"));Assert.True(r.Linhas[0].PodeImportar);}
    [Fact] public async Task Unidade_inexistente(){var r=await Validate(Row(2,"A1","Produto",unidade:"CX"));AssertCode(r,"IMP_CADASTRO_INEXISTENTE");}
    [Fact] public async Task Codigo_barras_preserva_zeros(){var r=await Validate(Row(2,"A1","Produto",barras:"001234"));Assert.Equal("001234",r.Linhas[0].ValoresConvertidos["codigoBarras"]);}
    [Fact] public async Task Linha_vazia_ignorada(){var r=await Validate(new LinhaPlanilhaImportada(2,new Dictionary<string,string?>()));Assert.Equal(1,r.QuantidadeIgnoradas);Assert.Empty(r.Linhas);}
    [Fact] public async Task Produto_existente_com_alteracao(){var existing=Product("A1","Antigo");var r=await Validate([Row(2,"A1","Novo")],[existing]);Assert.Equal(TipoOperacaoImportacao.Atualizar,r.Linhas[0].Operacao);Assert.NotEmpty(r.Linhas[0].Alteracoes);}
    [Fact] public async Task Produto_existente_sem_alteracao_ignorado(){var existing=Product("A1","Produto");var r=await Validate([Row(2,"A1","Produto")],[existing]);Assert.Equal(TipoOperacaoImportacao.Ignorar,r.Linhas[0].Operacao);}
    [Fact] public async Task Importacao_parcial_permite_validas(){var r=await Validate(Row(2,"A1","Ok"),Row(3,"","Erro"));Assert.True(r.PodeImportar);Assert.Equal(1,r.QuantidadeValida);}

    private Task<ResultadoValidacaoImportacao> Validate(params LinhaPlanilhaImportada[] rows)=>Validate(rows,[]);
    private Task<ResultadoValidacaoImportacao> Validate(IReadOnlyList<LinhaPlanilhaImportada> rows,IReadOnlyList<ProdutoExistenteImportacao> existing)=>service.ValidarAsync(new(Guid.NewGuid(),Guid.NewGuid(),null,rows,Mapping,new(),existing,new Dictionary<string,Guid>{{"un",Guid.Parse("11111111-1111-1111-1111-111111111111")}}));
    private static LinhaPlanilhaImportada Row(int line,string code,string name,string unidade="UN",string? custo=null,string? venda="10",string? estoque=null,string? tipo="próprio",string? barras=null)=>new(line,new Dictionary<string,string?>{{"Código",code},{"Descrição",name},{"Un",unidade},{"Custo",custo},{"Venda",venda},{"Estoque",estoque},{"Tipo",tipo},{"Barras",barras}});
    private static ProdutoExistenteImportacao Product(string code,string name)=>new(Guid.NewGuid(),code,name,null,0,10,Guid.Parse("11111111-1111-1111-1111-111111111111"),true);
    private static void AssertCode(ResultadoValidacaoImportacao r,string code)=>Assert.Contains(r.Linhas.SelectMany(x=>x.Erros),x=>x.Codigo==code);
}
