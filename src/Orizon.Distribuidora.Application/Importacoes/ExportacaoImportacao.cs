using Orizon.Distribuidora.Domain.Enums;
namespace Orizon.Distribuidora.Application.Importacoes;
public enum FiltroExportacaoImportacao { Todos=0, Inseridos, Atualizados, SemAlteracao, Ignorados, Bloqueados, Falhas, Avisos, Erros }
public sealed record ArquivoExportacaoImportacao(byte[] Conteudo,string NomeArquivo,string ContentType);
public sealed record ResumoExportacaoImportacao(Guid ImportacaoId,string Arquivo,string Empresa,string Usuario,DateTimeOffset DataImportacao,DateTimeOffset? DataExecucao,TimeSpan? Duracao,int Total,int Inseridos,int Atualizados,int SemAlteracao,int Ignorados,int Bloqueados,int Falhas,StatusImportacao Status,string? Modelo);
public sealed record LinhaExportacaoImportacao(int Linha,string? Codigo,string? Descricao,string? CodigoBarras,string? TipoProduto,OperacaoExecucaoImportacao Operacao,StatusLinhaImportacao Status,string? Mensagem,string? Campo,string? CodigoErro,Guid? ProdutoId,string Empresa,string Usuario,DateTimeOffset Data,string ValorOriginal,string? ValorPersistido,string? Observacoes,bool PossuiAviso,bool PossuiErro);
public sealed record DadosExportacaoImportacao(ResumoExportacaoImportacao Resumo,IReadOnlyList<LinhaExportacaoImportacao> Linhas);
