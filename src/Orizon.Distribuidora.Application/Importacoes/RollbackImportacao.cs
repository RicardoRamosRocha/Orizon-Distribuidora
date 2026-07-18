using Orizon.Distribuidora.Domain.Enums;
namespace Orizon.Distribuidora.Application.Importacoes;
public sealed record AnaliseRollbackImportacao(Guid ImportacaoId,bool Permitido,int ProdutosCriados,int ProdutosAlterados,int ProdutosBloqueados,TimeSpan TempoEstimado,IReadOnlyList<string> Riscos);
public sealed record ResultadoRollbackItem(int Linha,Guid? ProdutoId,string Operacao,bool Revertido,string Mensagem);
public sealed record ResultadoRollbackImportacao(Guid ImportacaoId,int ProdutosRemovidos,int ProdutosRestaurados,int ProdutosBloqueados,int Falhas,DateTimeOffset Inicio,DateTimeOffset Termino,StatusImportacao Status,IReadOnlyList<string> Mensagens,IReadOnlyList<ResultadoRollbackItem> Itens){public TimeSpan Duracao=>Termino-Inicio;}
public sealed class RollbackImportacaoException(string message):InvalidOperationException(message);
