using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ProductGridPreference : CompanyOwnedAuditableEntity
{
    private ProductGridPreference() { }

    public ProductGridPreference(Guid companyId, Guid userId, string stateJson) : base(companyId)
    {
        UserId = userId;
        SetState(stateJson);
    }

    public Guid UserId { get; private set; }
    public string StateJson { get; private set; } = "{}";

    public void SetState(string stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson) || stateJson.Length > 20_000)
            throw new ArgumentException("Preferencia de grade invalida.", nameof(stateJson));
        StateJson = stateJson;
    }
}
