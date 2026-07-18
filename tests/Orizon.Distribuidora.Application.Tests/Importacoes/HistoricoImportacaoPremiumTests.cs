using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Enums;
namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class HistoricoImportacaoPremiumTests
{
    [Theory][InlineData(25)][InlineData(50)][InlineData(100)][InlineData(200)] public void Aceita_tamanhos_de_pagina_permitidos(int size)=>Assert.Equal(size,HistoricoImportacaoPolicy.NormalizarTamanhoPagina(size));
    [Theory][InlineData(0)][InlineData(10)][InlineData(500)] public void Corrige_tamanho_de_pagina_invalido(int size)=>Assert.Equal(25,HistoricoImportacaoPolicy.NormalizarTamanhoPagina(size));
    [Fact] public void Pagina_nunca_e_menor_que_um()=>Assert.Equal(1,HistoricoImportacaoPolicy.NormalizarPagina(-2));
    [Fact] public void Pesquisa_localiza_arquivo()=>Assert.True(HistoricoImportacaoPolicy.CorrespondePesquisa(Item(),"PRODUTOS"));
    [Fact] public void Pesquisa_localiza_usuario()=>Assert.True(HistoricoImportacaoPolicy.CorrespondePesquisa(Item(),"maria"));
    [Fact] public void Pesquisa_localiza_codigo(){var x=Item();Assert.True(HistoricoImportacaoPolicy.CorrespondePesquisa(x,x.Id.ToString()));}
    [Fact] public void Pesquisa_rejeita_item_sem_correspondencia()=>Assert.False(HistoricoImportacaoPolicy.CorrespondePesquisa(Item(),"inexistente"));
    [Fact] public void Bloqueia_empresa_incorreta()=>Assert.Throws<UnauthorizedAccessException>(()=>HistoricoImportacaoPolicy.GarantirEmpresa(Guid.NewGuid(),Guid.NewGuid()));
    [Fact] public void Aceita_empresa_correta(){var id=Guid.NewGuid();HistoricoImportacaoPolicy.GarantirEmpresa(id,id);}
    [Fact] public void Nao_exclui_importacao_em_processamento()=>Assert.False(HistoricoImportacaoPolicy.PodeExcluir(StatusImportacao.Importando));
    [Fact] public void Permite_excluir_importacao_concluida()=>Assert.True(HistoricoImportacaoPolicy.PodeExcluir(StatusImportacao.Concluida));
    [Fact] public void Dashboard_preserva_series_independentes(){var d=new HistoricoDashboardDto([new("2026-01-01",2)],[new("2026-01",10)],[new("IMP",1)],[new("2026-01",3)]);Assert.Equal(4,new[]{d.ImportacoesPorDia.Count,d.ProdutosPorMes.Count,d.FalhasPorCategoria.Count,d.TempoMedioPorMes.Count}.Sum());}
    [Fact] public void Indicadores_preservam_totais(){var x=new HistoricoIndicadoresDto(3,10,4,1,TimeSpan.FromSeconds(2),DateTimeOffset.Now,8,"Maria");Assert.Equal(10,x.ProdutosImportados);Assert.Equal("Maria",x.UsuarioMaisAtivo);}
    [Fact] public void Detalhes_separam_erros_e_avisos(){var x=new HistoricoImportacaoDetalhesDto(Item(),["Erro"],["Aviso"]);Assert.Single(x.ResumoErros);Assert.Single(x.ResumoAvisos);}
    private static HistoricoImportacaoLinhaDto Item()=>new(Guid.NewGuid(),"produtos.xlsx","Orizon","Maria",DateTimeOffset.Now,DateTimeOffset.Now,TimeSpan.FromSeconds(1),10,5,2,1,1,1,0,1,StatusImportacao.Concluida,null,null,null);
}
