using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ImportacaoItem : CompanyOwnedAuditableEntity
{
    private readonly List<ImportacaoErro> erros = [];

    private ImportacaoItem()
    {
    }

    public ImportacaoItem(
        Guid companyId,
        Guid importacaoHistoricoId,
        int numeroLinha,
        string dadosOriginaisJson)
        : base(companyId)
    {
        ImportacaoHistoricoId = importacaoHistoricoId == Guid.Empty
            ? throw new ArgumentException("O histórico da importação é obrigatório.", nameof(importacaoHistoricoId))
            : importacaoHistoricoId;
        SetNumeroLinha(numeroLinha);
        DadosOriginaisJson = string.IsNullOrWhiteSpace(dadosOriginaisJson)
            ? "{}"
            : dadosOriginaisJson.Trim();
        Status = StatusLinhaImportacao.Pendente;
    }

    public Guid ImportacaoHistoricoId { get; private set; }

    public ImportacaoHistorico? ImportacaoHistorico { get; private set; }

    public int NumeroLinha { get; private set; }

    public StatusLinhaImportacao Status { get; private set; }

    public string DadosOriginaisJson { get; private set; } = "{}";

    public string? DadosNormalizadosJson { get; private set; }

    public Guid? ProdutoId { get; private set; }
    public OperacaoExecucaoImportacao OperacaoExecucao { get; private set; }
    public DateTimeOffset? ExecutadoEm { get; private set; }
    public string? MensagemExecucao { get; private set; }
    public string? AlteracoesAplicadasJson { get; private set; }
    public Guid? ChaveIdempotencia { get; private set; }
    public DateTimeOffset? RollbackExecutadoEm { get; private set; }
    public string? MensagemRollback { get; private set; }
    public bool Revertido { get; private set; }

    public Product? Produto { get; private set; }

    public IReadOnlyCollection<ImportacaoErro> Erros => erros.AsReadOnly();

    public void MarcarComoValida(string? dadosNormalizadosJson = null)
    {
        Status = StatusLinhaImportacao.Valida;
        DadosNormalizadosJson = string.IsNullOrWhiteSpace(dadosNormalizadosJson)
            ? null
            : dadosNormalizadosJson.Trim();
    }

    public void MarcarComErro()
    {
        Status = StatusLinhaImportacao.ComErro;
    }

    public void MarcarComoImportada(Guid produtoId)
    {
        ProdutoId = produtoId == Guid.Empty
            ? throw new ArgumentException("O produto importado é obrigatório.", nameof(produtoId))
            : produtoId;
        Status = StatusLinhaImportacao.Importada;
    }

    public void Ignorar()
    {
        Status = StatusLinhaImportacao.Ignorada;
    }
    public void PrepararExecucao(OperacaoExecucaoImportacao operacao){OperacaoExecucao=operacao;ChaveIdempotencia??=Guid.NewGuid();}
    public void ConcluirExecucao(StatusLinhaImportacao status, Guid? produtoId, string? mensagem=null, string? alteracoesJson=null){if(status is not(StatusLinhaImportacao.Inserida or StatusLinhaImportacao.Atualizada or StatusLinhaImportacao.SemAlteracao or StatusLinhaImportacao.Ignorada or StatusLinhaImportacao.Bloqueada or StatusLinhaImportacao.Falhou))throw new ArgumentException("Status final inválido.",nameof(status));Status=status;ProdutoId=produtoId;MensagemExecucao=string.IsNullOrWhiteSpace(mensagem)?null:mensagem.Trim();AlteracoesAplicadasJson=string.IsNullOrWhiteSpace(alteracoesJson)?null:alteracoesJson;ExecutadoEm=DateTimeOffset.UtcNow;}
    public void RegistrarRollback(bool revertido,string mensagem){Revertido=revertido;MensagemRollback=string.IsNullOrWhiteSpace(mensagem)?null:mensagem.Trim();RollbackExecutadoEm=DateTimeOffset.UtcNow;}

    private void SetNumeroLinha(int numeroLinha)
    {
        if (numeroLinha <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numeroLinha), "O número da linha deve ser maior que zero.");
        }

        NumeroLinha = numeroLinha;
    }
}
