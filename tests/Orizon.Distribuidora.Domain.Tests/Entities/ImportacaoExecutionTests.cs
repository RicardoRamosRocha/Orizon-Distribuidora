using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class ImportacaoExecutionTests
{
    [Fact] public void Inicia_execucao_somente_quando_pronta(){var h=Ready();var token=h.IniciarExecucao(Guid.NewGuid());Assert.NotEqual(Guid.Empty,token);Assert.Equal(StatusImportacao.Importando,h.Status);}
    [Fact] public void Impede_execucao_duplicada(){var h=Ready();h.IniciarExecucao(null);Assert.Throws<InvalidOperationException>(()=>h.IniciarExecucao(null));}
    [Fact] public void Transicao_renova_token_de_concorrencia(){var h=Ready();var token=h.ConcurrencyToken;h.IniciarExecucao(null);Assert.NotEqual(token,h.ConcurrencyToken);}
    [Fact] public void Impede_cancelamento_durante_execucao(){var h=Importing();Assert.Throws<InvalidOperationException>(()=>h.Cancelar());Assert.Equal(StatusImportacao.Importando,h.Status);}
    [Fact] public void Permite_cancelamento_antes_da_execucao(){var h=Ready();h.Cancelar();Assert.Equal(StatusImportacao.Cancelada,h.Status);}
    [Fact] public void Impede_revalidacao_durante_execucao(){var h=Importing();Assert.Throws<InvalidOperationException>(()=>h.RegistrarValidacao(2,2,0,0,2,0,0,0,0,true,null,"{}"));Assert.Equal(StatusImportacao.Importando,h.Status);}
    [Fact] public void Permite_revalidacao_antes_da_execucao(){var h=Ready();h.RegistrarValidacao(2,1,1,0,1,0,0,0,0,false,null,"{}");Assert.Equal(StatusImportacao.ValidacaoComErros,h.Status);}
    [Fact] public void Impede_cancelamento_de_importacao_concluida(){var h=Importing();h.FinalizarExecucao(2,2,0,0,0,0,0);Assert.Throws<InvalidOperationException>(()=>h.Cancelar());Assert.Equal(StatusImportacao.Concluida,h.Status);}
    [Fact] public void Impede_revalidacao_de_importacao_concluida(){var h=Importing();h.FinalizarExecucao(2,2,0,0,0,0,0);Assert.Throws<InvalidOperationException>(()=>h.RegistrarValidacao(2,2,0,0,2,0,0,0,0,true,null,"{}"));Assert.Equal(StatusImportacao.Concluida,h.Status);}
    [Fact] public void Impede_finalizacao_fora_de_execucao(){var h=Ready();Assert.Throws<InvalidOperationException>(()=>h.FinalizarExecucao(2,2,0,0,0,0,0));Assert.Equal(StatusImportacao.ProntaParaImportar,h.Status);}
    [Fact] public void Finaliza_com_sucesso(){var h=Ready();h.IniciarExecucao(null);h.FinalizarExecucao(3,2,1,0,0,0,0);Assert.Equal(StatusImportacao.Concluida,h.Status);Assert.Equal(3,h.LinhasImportadas);}
    [Fact] public void Finaliza_parcialmente(){var h=Ready();h.IniciarExecucao(null);h.FinalizarExecucao(3,1,0,0,0,1,1);Assert.Equal(StatusImportacao.ConcluidaParcialmente,h.Status);}
    [Fact] public void Finaliza_falha_sem_sucessos(){var h=Ready();h.IniciarExecucao(null);h.FinalizarExecucao(2,0,0,0,0,1,1);Assert.Equal(StatusImportacao.Falhou,h.Status);}
    [Fact] public void Contadores_sao_consolidados(){var h=Ready();h.IniciarExecucao(null);h.FinalizarExecucao(10,2,3,1,1,2,1);Assert.Equal(2,h.ProdutosInseridos);Assert.Equal(3,h.ProdutosAtualizados);Assert.Equal(1,h.SemAlteracao);Assert.Equal(2,h.ItensBloqueados);}
    [Fact] public void Item_recebe_chave_idempotente_uma_vez(){var i=Item();i.PrepararExecucao(OperacaoExecucaoImportacao.Inserir);var key=i.ChaveIdempotencia;i.PrepararExecucao(OperacaoExecucaoImportacao.Inserir);Assert.Equal(key,i.ChaveIdempotencia);}
    [Fact] public void Item_inserido_registra_produto(){var i=Item();var p=Guid.NewGuid();i.PrepararExecucao(OperacaoExecucaoImportacao.Inserir);i.ConcluirExecucao(StatusLinhaImportacao.Inserida,p,"ok");Assert.Equal(p,i.ProdutoId);Assert.NotNull(i.ExecutadoEm);}
    [Fact] public void Item_atualizado_preserva_alteracoes(){var i=Item();i.PrepararExecucao(OperacaoExecucaoImportacao.Atualizar);i.ConcluirExecucao(StatusLinhaImportacao.Atualizada,Guid.NewGuid(),"ok","{\"preco\":true}");Assert.Contains("preco",i.AlteracoesAplicadasJson);}
    [Fact] public void Item_ignorado_preserva_produto_e_json_normalizado_ao_concluir_execucao(){var i=Item();var p=Guid.NewGuid();i.Ignorar("{\"codigo\":\"P1\"}",p);i.PrepararExecucao(OperacaoExecucaoImportacao.Ignorar);i.ConcluirExecucao(StatusLinhaImportacao.Ignorada,null,"ignorado");Assert.Equal(p,i.ProdutoId);Assert.Contains("P1",i.DadosNormalizadosJson);}
    [Fact] public void Item_rejeita_status_nao_final(){var i=Item();Assert.Throws<ArgumentException>(()=>i.ConcluirExecucao(StatusLinhaImportacao.Valida,null));}

    private static ImportacaoHistorico Ready(){var h=new ImportacaoHistorico(Guid.NewGuid(),"a.xlsx",TipoArquivoImportacao.Excel,10);h.RegistrarValidacao(2,2,0,0,2,0,0,0,0,true,null,"{}");return h;}
    private static ImportacaoHistorico Importing(){var h=Ready();h.IniciarExecucao(null);return h;}
    private static ImportacaoItem Item()=>new(Guid.NewGuid(),Guid.NewGuid(),2,"{}");
}
