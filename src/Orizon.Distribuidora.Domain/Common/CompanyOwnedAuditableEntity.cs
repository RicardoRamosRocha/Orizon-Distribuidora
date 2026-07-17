using Orizon.Distribuidora.Domain.Interfaces;

namespace Orizon.Distribuidora.Domain.Common;

public abstract class CompanyOwnedAuditableEntity :
    AuditableEntity,
    ICompanyOwnedEntity
{
    protected CompanyOwnedAuditableEntity()
    {
    }

    protected CompanyOwnedAuditableEntity(Guid companyId)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException(
                "A empresa é obrigatória.",
                nameof(companyId));
        }

        CompanyId = companyId;
    }

    public Guid CompanyId { get; set; }
}
