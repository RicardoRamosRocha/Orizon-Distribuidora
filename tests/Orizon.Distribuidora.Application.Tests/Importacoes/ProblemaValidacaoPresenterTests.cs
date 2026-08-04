using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ProblemaValidacaoPresenterTests
{
    [Fact]
    public void Agrupa_ocorrencias_iguais_e_conta_produtos_distintos()
    {
        var problemas = ProblemaValidacaoPresenter.Agrupar([
            Ocorrencia(2, "unidade", "IMP_CADASTRO_INEXISTENTE", SeveridadeValidacao.Erro),
            Ocorrencia(2, "unidade", "IMP_CADASTRO_INEXISTENTE", SeveridadeValidacao.Erro),
            Ocorrencia(5, "unidade", "IMP_CADASTRO_INEXISTENTE", SeveridadeValidacao.Erro)
        ]);

        var problema = Assert.Single(problemas);
        Assert.Equal(2, problema.QuantidadeLinhasAfetadas);
        Assert.Equal([2, 5], problema.LinhasAfetadas);
        Assert.Contains("2 produtos foram afetados", problema.Descricao);
    }

    [Fact]
    public void Coloca_bloqueios_antes_dos_avisos()
    {
        var problemas = ProblemaValidacaoPresenter.Agrupar([
            Ocorrencia(1, "precoVenda", "IMP_PRECO_ZERADO", SeveridadeValidacao.Aviso),
            Ocorrencia(2, "codigo", "IMP_CAMPO_OBRIGATORIO", SeveridadeValidacao.Erro)
        ]);

        Assert.True(problemas[0].Bloqueante);
        Assert.False(problemas[1].Bloqueante);
    }

    [Theory]
    [InlineData("categoria", "Criar Categoria")]
    [InlineData("marca", "Criar Marca")]
    [InlineData("fornecedor", "Criar Fornecedor")]
    [InlineData("unidade", "Criar Unidade")]
    public void Cadastro_inexistente_oferece_criacao_e_escolha(string campo, string rotuloCriacao)
    {
        var problema = ProblemaValidacaoPresenter.Apresentar(
            Ocorrencia(3, campo, "IMP_CADASTRO_INEXISTENTE", SeveridadeValidacao.Erro));

        Assert.Contains(problema.AcoesRapidas, x => x.Rotulo == rotuloCriacao && x.CriarNovo);
        Assert.Contains(problema.AcoesRapidas, x => x.Rotulo == "Escolher existente" && !x.CriarNovo);
        Assert.DoesNotContain("IMP_", problema.Titulo);
        Assert.DoesNotContain("IMP_", problema.Causa);
    }

    private static ErroValidacaoImportacao Ocorrencia(
        int linha, string campo, string codigo, SeveridadeValidacao severidade) =>
        new(linha, campo, "valor", codigo, "mensagem técnica", severidade, DateTimeOffset.UtcNow);
}
