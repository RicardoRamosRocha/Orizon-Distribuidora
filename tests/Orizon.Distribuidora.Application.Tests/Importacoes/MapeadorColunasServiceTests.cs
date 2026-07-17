using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class MapeadorColunasServiceTests
{
    private readonly MapeadorColunasService service = new();

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
    public async Task Nao_utiliza_mesma_coluna_duas_vezes()
    {
        var result = await service.MapearAsync(["Produto"]);
        Assert.Single(result.Colunas);
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
        Assert.Equal(23, CatalogoCamposProdutoImportacao.Campos.Count);
        Assert.All(CatalogoCamposProdutoImportacao.Campos, x => Assert.True(x.AceitaImportacao));
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
