using Orizon.Distribuidora.Application.Importacoes;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface IValidadorImportacaoService
{
    Task<ResultadoValidacaoImportacao> ValidarAsync(
        PlanilhaImportada planilha,
        MapeamentoColunasImportacao mapeamento,
        CancellationToken cancellationToken = default);
}

public interface IValidadorDadosImportacaoService
{
    Task<ResultadoValidacaoImportacao> ValidarAsync(ContextoValidacaoImportacao contexto, CancellationToken cancellationToken = default);
}

public interface IContextoValidacaoImportacaoService
{
    Task<ContextoValidacaoImportacao> PrepararAsync(Guid importacaoId, Guid empresaId, Guid? usuarioId, IReadOnlyList<LinhaPlanilhaImportada> linhas, MapeamentoColunasImportacao mapeamento, OpcoesValidacaoImportacao opcoes, CancellationToken cancellationToken = default);
}
