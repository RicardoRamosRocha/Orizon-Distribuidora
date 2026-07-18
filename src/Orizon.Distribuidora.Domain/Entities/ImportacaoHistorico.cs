using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ImportacaoHistorico : CompanyOwnedAuditableEntity
{
    private readonly List<ImportacaoItem> itens = [];
    private readonly List<ImportacaoErro> erros = [];

    private ImportacaoHistorico()
    {
    }

    public ImportacaoHistorico(
        Guid companyId,
        string nomeArquivo,
        TipoArquivoImportacao tipoArquivo,
        long tamanhoArquivoBytes,
        Guid? modeloImportacaoId = null)
        : base(companyId)
    {
        SetArquivo(nomeArquivo, tamanhoArquivoBytes);
        TipoArquivo = tipoArquivo;
        ModeloImportacaoId = modeloImportacaoId;
        Status = StatusImportacao.AguardandoProcessamento;
    }

    public Guid? ModeloImportacaoId { get; private set; }

    public ModeloImportacao? ModeloImportacao { get; private set; }

    public string NomeArquivo { get; private set; } = string.Empty;

    public TipoArquivoImportacao TipoArquivo { get; private set; }

    public long TamanhoArquivoBytes { get; private set; }

    public StatusImportacao Status { get; private set; }

    public int TotalLinhas { get; private set; }

    public int LinhasValidas { get; private set; }

    public int LinhasComErro { get; private set; }

    public int LinhasImportadas { get; private set; }

    public DateTimeOffset? IniciadoEm { get; private set; }

    public DateTimeOffset? FinalizadoEm { get; private set; }

    public string? Observacoes { get; private set; }
    public int LinhasComAviso { get; private set; }
    public int ProdutosNovos { get; private set; }
    public int ProdutosExistentes { get; private set; }
    public int ProdutosAtualizaveis { get; private set; }
    public int LinhasDuplicadas { get; private set; }
    public int LinhasIgnoradas { get; private set; }
    public Guid? UsuarioValidacaoId { get; private set; }
    public string? OpcoesValidacaoJson { get; private set; }
    public int ProdutosInseridos { get; private set; }
    public int ProdutosAtualizados { get; private set; }
    public int SemAlteracao { get; private set; }
    public int ItensBloqueados { get; private set; }
    public int FalhasExecucao { get; private set; }
    public Guid? UsuarioExecutorId { get; private set; }
    public Guid? TokenExecucao { get; private set; }
    public DateTimeOffset? RollbackIniciadoEm { get; private set; }
    public DateTimeOffset? RollbackFinalizadoEm { get; private set; }
    public Guid? UsuarioRollbackId { get; private set; }
    public int ProdutosRemovidosRollback { get; private set; }
    public int ProdutosRestauradosRollback { get; private set; }
    public int ProdutosBloqueadosRollback { get; private set; }
    public int FalhasRollback { get; private set; }
    public string? ObservacoesRollback { get; private set; }

    public IReadOnlyCollection<ImportacaoItem> Itens => itens.AsReadOnly();

    public IReadOnlyCollection<ImportacaoErro> Erros => erros.AsReadOnly();

    public void Iniciar()
    {
        Status = StatusImportacao.EmProcessamento;
        IniciadoEm = DateTimeOffset.UtcNow;
    }

    public void Finalizar(
        int totalLinhas,
        int linhasValidas,
        int linhasComErro,
        int linhasImportadas,
        string? observacoes = null)
    {
        if (totalLinhas < 0 || linhasValidas < 0 || linhasComErro < 0 || linhasImportadas < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLinhas), "Os totais da importação não podem ser negativos.");
        }

        TotalLinhas = totalLinhas;
        LinhasValidas = linhasValidas;
        LinhasComErro = linhasComErro;
        LinhasImportadas = linhasImportadas;
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        Status = linhasComErro > 0 ? StatusImportacao.ProcessadaComErros : StatusImportacao.ProcessadaComSucesso;
        FinalizadoEm = DateTimeOffset.UtcNow;
    }

    public void Cancelar(string? observacoes = null)
    {
        Status = StatusImportacao.Cancelada;
        Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
        FinalizadoEm = DateTimeOffset.UtcNow;
    }

    public void RegistrarValidacao(int total, int validas, int erros, int avisos, int novos, int existentes, int atualizaveis, int duplicadas, int ignoradas, bool podeImportar, Guid? usuarioId, string opcoesJson)
    {
        TotalLinhas=total;LinhasValidas=validas;LinhasComErro=erros;LinhasComAviso=avisos;ProdutosNovos=novos;ProdutosExistentes=existentes;ProdutosAtualizaveis=atualizaveis;LinhasDuplicadas=duplicadas;LinhasIgnoradas=ignoradas;UsuarioValidacaoId=usuarioId;OpcoesValidacaoJson=opcoesJson;IniciadoEm??=DateTimeOffset.UtcNow;FinalizadoEm=DateTimeOffset.UtcNow;Status=podeImportar?StatusImportacao.ProntaParaImportar:StatusImportacao.ValidacaoComErros;
    }
    public Guid IniciarExecucao(Guid? usuarioId){if(Status!=StatusImportacao.ProntaParaImportar)throw new InvalidOperationException("A importação não está pronta para execução.");Status=StatusImportacao.Importando;UsuarioExecutorId=usuarioId;TokenExecucao=Guid.NewGuid();IniciadoEm=DateTimeOffset.UtcNow;FinalizadoEm=null;return TokenExecucao.Value;}
    public void FinalizarExecucao(int processados,int inseridos,int atualizados,int semAlteracao,int ignorados,int bloqueados,int falhas){TotalLinhas=processados;ProdutosInseridos=inseridos;ProdutosAtualizados=atualizados;SemAlteracao=semAlteracao;LinhasIgnoradas=ignorados;ItensBloqueados=bloqueados;FalhasExecucao=falhas;LinhasImportadas=inseridos+atualizados;FinalizadoEm=DateTimeOffset.UtcNow;Status=falhas==0&&bloqueados==0?StatusImportacao.Concluida:(inseridos+atualizados)>0?StatusImportacao.ConcluidaParcialmente:StatusImportacao.Falhou;TokenExecucao=null;}
    public void IniciarRollback(Guid? usuarioId){if(Status is not(StatusImportacao.Concluida or StatusImportacao.ConcluidaParcialmente))throw new InvalidOperationException("A importação não permite rollback.");Status=StatusImportacao.RollbackEmAndamento;UsuarioRollbackId=usuarioId;RollbackIniciadoEm=DateTimeOffset.UtcNow;RollbackFinalizadoEm=null;}
    public void FinalizarRollback(int removidos,int restaurados,int bloqueados,int falhas,string? observacoes){ProdutosRemovidosRollback=removidos;ProdutosRestauradosRollback=restaurados;ProdutosBloqueadosRollback=bloqueados;FalhasRollback=falhas;ObservacoesRollback=string.IsNullOrWhiteSpace(observacoes)?null:observacoes.Trim();RollbackFinalizadoEm=DateTimeOffset.UtcNow;Status=falhas==0&&bloqueados==0?StatusImportacao.Revertida:(removidos+restaurados)>0?StatusImportacao.RevertidaParcialmente:StatusImportacao.RollbackFalhou;}

    private void SetArquivo(string nomeArquivo, long tamanhoArquivoBytes)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo))
        {
            throw new ArgumentException("O nome do arquivo é obrigatório.", nameof(nomeArquivo));
        }

        if (tamanhoArquivoBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tamanhoArquivoBytes), "O tamanho do arquivo não pode ser negativo.");
        }

        NomeArquivo = nomeArquivo.Trim();
        TamanhoArquivoBytes = tamanhoArquivoBytes;
    }
}
