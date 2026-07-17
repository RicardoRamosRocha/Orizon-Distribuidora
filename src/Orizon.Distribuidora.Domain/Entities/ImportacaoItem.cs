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

    private void SetNumeroLinha(int numeroLinha)
    {
        if (numeroLinha <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numeroLinha), "O número da linha deve ser maior que zero.");
        }

        NumeroLinha = numeroLinha;
    }
}
