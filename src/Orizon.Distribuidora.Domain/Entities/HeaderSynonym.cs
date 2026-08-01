using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class HeaderSynonym : AuditableEntity
{
    private HeaderSynonym()
    {
    }

    public HeaderSynonym(
        string campoDestino,
        string sinonimo,
        int prioridade = 0,
        string origem = "Banco",
        Guid? companyId = null,
        bool ativo = true)
    {
        SetCampoDestino(campoDestino);
        SetSinonimo(sinonimo);
        SetOrigem(origem);
        Prioridade = prioridade;
        CompanyId = companyId;
        Ativo = ativo;
    }

    public string CampoDestino { get; private set; } = string.Empty;
    public string Sinonimo { get; private set; } = string.Empty;
    public int Prioridade { get; private set; }
    public bool Ativo { get; private set; } = true;
    public string Origem { get; private set; } = string.Empty;
    public Guid? CompanyId { get; private set; }

    public void Atualizar(string sinonimo, int prioridade, bool ativo, string origem)
    {
        SetSinonimo(sinonimo);
        SetOrigem(origem);
        Prioridade = prioridade;
        Ativo = ativo;
    }

    private void SetCampoDestino(string campoDestino)
    {
        if (string.IsNullOrWhiteSpace(campoDestino))
            throw new ArgumentException("O campo de destino é obrigatório.", nameof(campoDestino));
        CampoDestino = campoDestino.Trim();
    }

    private void SetSinonimo(string sinonimo)
    {
        if (string.IsNullOrWhiteSpace(sinonimo))
            throw new ArgumentException("O sinônimo é obrigatório.", nameof(sinonimo));
        Sinonimo = sinonimo.Trim();
    }

    private void SetOrigem(string origem)
    {
        if (string.IsNullOrWhiteSpace(origem))
            throw new ArgumentException("A origem é obrigatória.", nameof(origem));
        Origem = origem.Trim();
    }
}
