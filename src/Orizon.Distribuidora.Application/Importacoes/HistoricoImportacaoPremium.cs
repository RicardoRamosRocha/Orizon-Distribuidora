using Orizon.Distribuidora.Domain.Enums;
namespace Orizon.Distribuidora.Application.Importacoes;
public sealed record ConsultaHistoricoImportacao(DateTimeOffset? Inicio=null,DateTimeOffset? Fim=null,Guid? UsuarioId=null,string? Arquivo=null,StatusImportacao? Status=null,Guid? ModeloId=null,bool? PossuiFalhas=null,bool? PossuiAvisos=null,bool? PossuiBloqueios=null,string? Pesquisa=null,int Pagina=1,int TamanhoPagina=25,string OrdenarPor="data",bool Descendente=true);
public sealed record HistoricoImportacaoLinhaDto(Guid Id,string Arquivo,string Empresa,string Usuario,DateTimeOffset DataImportacao,DateTimeOffset? DataExecucao,TimeSpan? Duracao,int TotalLinhas,int Inseridos,int Atualizados,int SemAlteracao,int Ignorados,int Bloqueados,int Falhas,int Avisos,StatusImportacao Status,Guid? ModeloId,string? Modelo,string? Versao);
public sealed record HistoricoIndicadoresDto(int TotalImportacoes,int ProdutosImportados,int ProdutosAtualizados,int Falhas,TimeSpan TempoMedio,DateTimeOffset? UltimaImportacao,int MaiorImportacao,string? UsuarioMaisAtivo);
public sealed record PaginaHistoricoImportacao(IReadOnlyList<HistoricoImportacaoLinhaDto> Itens,int Pagina,int TamanhoPagina,int TotalRegistros,int TotalPaginas,HistoricoIndicadoresDto Indicadores,IReadOnlyList<(Guid Id,string Nome)> Usuarios,IReadOnlyList<(Guid Id,string Nome)> Modelos);
public sealed record HistoricoImportacaoDetalhesDto(HistoricoImportacaoLinhaDto Importacao,IReadOnlyList<string> ResumoErros,IReadOnlyList<string> ResumoAvisos);
public sealed record PontoDashboardImportacao(string Rotulo,decimal Valor);
public sealed record HistoricoDashboardDto(IReadOnlyList<PontoDashboardImportacao> ImportacoesPorDia,IReadOnlyList<PontoDashboardImportacao> ProdutosPorMes,IReadOnlyList<PontoDashboardImportacao> FalhasPorCategoria,IReadOnlyList<PontoDashboardImportacao> TempoMedioPorMes);
public static class HistoricoImportacaoPolicy
{
    public static int NormalizarTamanhoPagina(int valor)=>new[]{25,50,100,200}.Contains(valor)?valor:25;
    public static int NormalizarPagina(int valor)=>Math.Max(1,valor);
    public static void GarantirEmpresa(Guid esperada,Guid encontrada){if(esperada==Guid.Empty||esperada!=encontrada)throw new UnauthorizedAccessException("Histórico não pertence à empresa atual.");}
    public static bool PodeExcluir(StatusImportacao status)=>status!=StatusImportacao.Importando;
    public static bool CorrespondePesquisa(HistoricoImportacaoLinhaDto item,string? pesquisa)=>string.IsNullOrWhiteSpace(pesquisa)||item.Arquivo.Contains(pesquisa,StringComparison.OrdinalIgnoreCase)||item.Usuario.Contains(pesquisa,StringComparison.OrdinalIgnoreCase)||item.Id.ToString().Equals(pesquisa,StringComparison.OrdinalIgnoreCase);
}
