using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
namespace Orizon.Distribuidora.Domain.Tests.Entities;
public sealed class ImportacaoRollbackTests
{
    [Fact] public void Inicia_rollback_de_importacao_concluida(){var h=Completed();h.IniciarRollback(Guid.NewGuid());Assert.Equal(StatusImportacao.RollbackEmAndamento,h.Status);Assert.NotNull(h.RollbackIniciadoEm);}
    [Fact] public void Bloqueia_rollback_de_estado_invalido(){var h=new ImportacaoHistorico(Guid.NewGuid(),"x.xlsx",TipoArquivoImportacao.Excel,1);Assert.Throws<InvalidOperationException>(()=>h.IniciarRollback(null));}
    [Fact] public void Bloqueia_rollback_duplicado(){var h=Completed();h.IniciarRollback(null);h.FinalizarRollback(1,0,0,0,null);Assert.Throws<InvalidOperationException>(()=>h.IniciarRollback(null));}
    [Fact] public void Rollback_completo_fica_revertido(){var h=Completed();h.IniciarRollback(null);h.FinalizarRollback(1,2,0,0,"ok");Assert.Equal(StatusImportacao.Revertida,h.Status);Assert.Equal(1,h.ProdutosRemovidosRollback);Assert.Equal(2,h.ProdutosRestauradosRollback);}
    [Fact] public void Rollback_parcial_registra_bloqueios(){var h=Completed();h.IniciarRollback(null);h.FinalizarRollback(1,0,1,0,"parcial");Assert.Equal(StatusImportacao.RevertidaParcialmente,h.Status);Assert.Equal(1,h.ProdutosBloqueadosRollback);}
    [Fact] public void Rollback_sem_sucesso_falha(){var h=Completed();h.IniciarRollback(null);h.FinalizarRollback(0,0,1,1,"falhou");Assert.Equal(StatusImportacao.RollbackFalhou,h.Status);}
    [Fact] public void Registra_usuario_e_tempo(){var user=Guid.NewGuid();var h=Completed();h.IniciarRollback(user);h.FinalizarRollback(1,0,0,0,null);Assert.Equal(user,h.UsuarioRollbackId);Assert.NotNull(h.RollbackFinalizadoEm);}
    [Fact] public void Item_criado_registra_soft_rollback(){var i=Item();i.RegistrarRollback(true,"Produto removido logicamente.");Assert.True(i.Revertido);Assert.NotNull(i.RollbackExecutadoEm);}
    [Fact] public void Item_atualizado_registra_restauracao(){var i=Item();i.RegistrarRollback(true,"Valores anteriores restaurados.");Assert.Contains("restaurados",i.MensagemRollback);}
    [Fact] public void Item_bloqueado_preserva_motivo(){var i=Item();i.RegistrarRollback(false,"Alterações posteriores.");Assert.False(i.Revertido);Assert.Equal("Alterações posteriores.",i.MensagemRollback);}
    [Fact] public void Finalizacao_preserva_observacoes_de_auditoria(){var h=Completed();h.IniciarRollback(null);h.FinalizarRollback(1,0,0,0,"Rollback integral");Assert.Equal("Rollback integral",h.ObservacoesRollback);}
    [Fact] public void Resultado_e_idempotente_por_status(){var h=Completed();h.IniciarRollback(null);h.FinalizarRollback(1,0,0,0,null);Assert.Equal(StatusImportacao.Revertida,h.Status);Assert.Throws<InvalidOperationException>(()=>h.IniciarRollback(null));}
    private static ImportacaoHistorico Completed(){var h=new ImportacaoHistorico(Guid.NewGuid(),"x.xlsx",TipoArquivoImportacao.Excel,1);h.RegistrarValidacao(1,1,0,0,1,0,0,0,0,true,null,"{}");h.IniciarExecucao(null);h.FinalizarExecucao(1,1,0,0,0,0,0);return h;}
    private static ImportacaoItem Item()=>new(Guid.NewGuid(),Guid.NewGuid(),2,"{}");
}
