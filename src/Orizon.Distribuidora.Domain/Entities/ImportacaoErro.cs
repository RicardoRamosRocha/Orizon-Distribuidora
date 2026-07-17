using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ImportacaoErro : CompanyOwnedAuditableEntity
{
    private ImportacaoErro()
    {
    }

    public ImportacaoErro(
        Guid companyId,
        Guid importacaoHistoricoId,
        string mensagem,
        Guid? importacaoItemId = null,
        int? numeroLinha = null,
        string? coluna = null,
        string? valorOriginal = null)
        : base(companyId)
    {
        ImportacaoHistoricoId = importacaoHistoricoId == Guid.Empty
            ? throw new ArgumentException("O histórico da importação é obrigatório.", nameof(importacaoHistoricoId))
            : importacaoHistoricoId;
        ImportacaoItemId = importacaoItemId;
        NumeroLinha = numeroLinha;
        Coluna = Normalize(coluna);
        ValorOriginal = Normalize(valorOriginal);
        SetMensagem(mensagem);
    }

    public Guid ImportacaoHistoricoId { get; private set; }

    public ImportacaoHistorico? ImportacaoHistorico { get; private set; }

    public Guid? ImportacaoItemId { get; private set; }

    public ImportacaoItem? ImportacaoItem { get; private set; }

    public int? NumeroLinha { get; private set; }

    public string? Coluna { get; private set; }

    public string? ValorOriginal { get; private set; }

    public string Mensagem { get; private set; } = string.Empty;

    private void SetMensagem(string mensagem)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
        {
            throw new ArgumentException("A mensagem do erro é obrigatória.", nameof(mensagem));
        }

        Mensagem = mensagem.Trim();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
