using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class MapeadorColunasServiceTests
{
    private readonly MapeadorColunasService service = new();
    private readonly SimilarityEngine similarityEngine = new();

    [Fact]
    public async Task Mapeia_acentos_espacos_e_sinonimos()
    {
        var result = await service.MapearAsync(["CÓD Produto", "Nome", "PV", "Fabricante", "UN"]);
        Assert.Equal("CÓD Produto", result.Colunas["codigo"]);
        Assert.Equal("Nome", result.Colunas["descricao"]);
        Assert.Equal("PV", result.Colunas["precoVenda"]);
        Assert.Equal("Fabricante", result.Colunas["marca"]);
    }

    [Fact]
    public async Task Biblioteca_central_reconhece_sinonimos_com_acentos_espacos_e_separadores()
    {
        var headers = new[]
        {
            "COD. PRODUTO", "Descricao__do Produto", "UN / MEDIDA", "PRECO--VENDA",
            "Saldo_Estoque", "Fabricante", "Departamento", "Sub-Categoria",
            "Grupo / Produto", "Fornecedor.Principal", "Parceiro / CNPJ", "N.C.M.", "OBSERVACOES"
        };

        var result = await service.MapearAsync(headers);

        Assert.Equal("COD. PRODUTO", result.Colunas["codigo"]);
        Assert.Equal("Descricao__do Produto", result.Colunas["descricao"]);
        Assert.Equal("UN / MEDIDA", result.Colunas["unidade"]);
        Assert.Equal("PRECO--VENDA", result.Colunas["precoVenda"]);
        Assert.Equal("Saldo_Estoque", result.Colunas["estoqueInicial"]);
        Assert.Equal("Fabricante", result.Colunas["marca"]);
        Assert.Equal("Departamento", result.Colunas["categoria"]);
        Assert.Equal("Sub-Categoria", result.Colunas["subcategoria"]);
        Assert.Equal("Grupo / Produto", result.Colunas["grupo"]);
        Assert.Equal("Fornecedor.Principal", result.Colunas["fornecedor"]);
        Assert.Equal("Parceiro / CNPJ", result.Colunas["parceiroCnpj"]);
        Assert.Equal("N.C.M.", result.Colunas["ncm"]);
        Assert.Equal("OBSERVACOES", result.Colunas["observacoes"]);
    }

    [Theory]
    [InlineData("  PRECO   DE   VENDA  ", "preco de venda")]
    [InlineData("N.C.M.", "ncm")]
    [InlineData("Sub-Categoria", "subcategoria")]
    [InlineData("Parceiro/CNPJ", "parceirocnpj")]
    public void Normalizacao_central_ignora_variacoes_de_cabecalho(string header, string expected)
    {
        Assert.Equal(expected, HeaderSynonymDictionary.Normalize(header));
    }

    [Theory]
    [InlineData("Descrição", "descricao", 100, RecognitionStrategy.Exact)]
    [InlineData("Fabricante", "marca", 100, RecognitionStrategy.Synonym)]
    public void Motor_classifica_reconhecimento_direto(
        string header,
        string expectedField,
        double expectedConfidence,
        RecognitionStrategy expectedStrategy)
    {
        var result = similarityEngine.Recognize(header)[0];

        Assert.Equal(expectedField, result.CampoDestino);
        Assert.Equal(expectedConfidence, result.Confidence);
        Assert.Equal(expectedStrategy, result.Strategy);
        Assert.Equal(header, result.MatchedHeader);
    }

    [Fact]
    public void Motor_ordena_sugestoes_de_cabecalho_nao_reconhecido_por_confianca()
    {
        var results = similarityEngine.Recognize("Descrico do Prodto");

        Assert.Equal("descricao", results[0].CampoDestino);
        Assert.Equal(RecognitionStrategy.Similarity, results[0].Strategy);
        Assert.InRange(results[0].Confidence, 80, 99.99);
        Assert.Equal(results.OrderByDescending(item => item.Confidence).ThenBy(item => item.CampoDestino).ToList(), results);
    }

    [Fact]
    public async Task Mapeador_utiliza_similaridade_para_cabecalhos_sem_sinonimo_direto()
    {
        var result = await service.MapearAsync(["Codgo do Prodto", "Descrico do Prodto", "Unidde", "Preco de Vnda"]);

        Assert.Equal("Codgo do Prodto", result.Colunas["codigo"]);
        Assert.Equal("Descrico do Prodto", result.Colunas["descricao"]);
        Assert.Equal("Unidde", result.Colunas["unidade"]);
        Assert.Equal("Preco de Vnda", result.Colunas["precoVenda"]);
        Assert.All(result.Confiancas!.Values, confidence => Assert.InRange(confidence, .72, 1));
    }

    [Fact]
    public async Task Nao_utiliza_mesma_coluna_duas_vezes()
    {
        var result = await service.MapearAsync(["Produto"]);
        Assert.Single(result.Colunas);
    }

    [Fact]
    public async Task Mapeia_caminho_feliz_preservando_cabecalhos_originais()
    {
        var headers = new[] { " Código ", "Descrição", "Unidade", "Preço de venda" };

        var result = await service.MapearAsync(headers);

        Assert.Equal(" Código ", result.Colunas["codigo"]);
        Assert.Equal("Descrição", result.Colunas["descricao"]);
        Assert.Equal("Unidade", result.Colunas["unidade"]);
        Assert.Equal("Preço de venda", result.Colunas["precoVenda"]);
    }

    [Fact]
    public async Task Nao_mapeia_automaticamente_quando_duas_colunas_sao_ambiguas()
    {
        var result = await service.MapearAsync(["Código", "SKU", "Descrição", "Unidade", "Preço de venda"]);

        Assert.False(result.Colunas.ContainsKey("codigo"));
        Assert.Equal(["Código", "SKU"], result.Conflitos!["codigo"]);
    }

    [Fact]
    public void Canonicaliza_chaves_do_catalogo_sem_perder_o_nome_da_coluna()
    {
        var result = MapeamentoColunasImportacao.Canonicalizar(
            new Dictionary<string, string> { ["CODIGO"] = " Código original " });

        Assert.Equal(" Código original ", result["codigo"]);
        Assert.False(result.ContainsKey("CODIGO"));
    }

    [Fact]
    public void Valida_obrigatorios_coluna_inexistente_e_repetida()
    {
        var result = ValidadorMapeamentoColunas.Validar(new Dictionary<string,string>{{"codigo","A"},{"descricao","A"},{"unidade","X"},{"precoVenda","P"}}, ["A","P"]);
        Assert.False(result.Valido);
        Assert.Contains(result.Erros, x => x.Mensagem.Contains("mais de uma vez"));
        Assert.Contains(result.Erros, x => x.Mensagem.Contains("não existe"));
    }

    [Fact]
    public void Catalogo_define_metadados_oficiais()
    {
        Assert.Equal(25, CatalogoCamposProdutoImportacao.Campos.Count);
        Assert.Equal(21, CatalogoCamposProdutoImportacao.Campos.Count(x => x.AceitaImportacao));
        Assert.All(CatalogoCamposProdutoImportacao.Campos, x => Assert.False(string.IsNullOrWhiteSpace(x.Destino)));
        Assert.All(CatalogoCamposProdutoImportacao.Campos.Where(x => !x.AceitaImportacao), x => Assert.False(string.IsNullOrWhiteSpace(x.MotivoIndisponibilidade)));
        Assert.Contains(CatalogoCamposProdutoImportacao.Campos, x => x.Chave == "codigo" && x.Obrigatorio);
    }

    [Fact]
    public void Detecta_tipo_incompativel_na_amostra()
    {
        IReadOnlyList<IReadOnlyDictionary<string,string?>> sample = [new Dictionary<string,string?>{{"Preço","abc"}}, new Dictionary<string,string?>{{"Preço","inválido"}}];
        var result = ValidadorMapeamentoColunas.Validar(new Dictionary<string,string>{{"codigo","Código"},{"descricao","Descrição"},{"unidade","Un"},{"precoVenda","Preço"}}, ["Código","Descrição","Un","Preço"], sample);
        Assert.Contains(result.Erros, x => x.Mensagem.Contains("incompatível"));
    }

    [Fact]
    public void Modelo_armazena_usuario_assinatura_e_padrao()
    {
        var user = Guid.NewGuid();
        var model = new ModeloImportacao(Guid.NewGuid(), "Meu modelo", TipoArquivoImportacao.Excel, "{}", user, "a|b", true);
        Assert.Equal(user, model.UsuarioId); Assert.True(model.Padrao); Assert.Equal("a|b", model.AssinaturaColunas);
        model.DefinirComoPadrao(false); Assert.False(model.Padrao);
    }
}
