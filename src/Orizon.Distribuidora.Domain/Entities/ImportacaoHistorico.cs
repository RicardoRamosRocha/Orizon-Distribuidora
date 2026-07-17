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
