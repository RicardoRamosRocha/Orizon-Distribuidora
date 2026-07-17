using Orizon.Distribuidora.Domain.Enums;
namespace Orizon.Distribuidora.Application.Importacoes;
public sealed record ResultadoExecucaoItem(int Linha,string? Codigo,string? Descricao,OperacaoExecucaoImportacao Operacao,StatusLinhaImportacao Status,Guid? ProdutoId,string? Mensagem);
public sealed record ResultadoExecucaoImportacao(Guid ImportacaoId,int TotalProcessado,int Inseridos,int Atualizados,int SemAlteracao,int Ignorados,int Bloqueados,int Falhas,int Avisos,DateTimeOffset Inicio,DateTimeOffset Termino,StatusImportacao StatusFinal,IReadOnlyList<string> Mensagens,IReadOnlyList<ResultadoExecucaoItem> Itens)
{public TimeSpan Duracao=>Termino-Inicio;}
public sealed class ImportacaoExecucaoException(string message) : InvalidOperationException(message);
