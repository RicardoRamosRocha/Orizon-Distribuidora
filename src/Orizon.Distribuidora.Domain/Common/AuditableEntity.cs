using Orizon.Distribuidora.Domain.Interfaces;

namespace Orizon.Distribuidora.Domain.Common;

public abstract class AuditableEntity :
    Entity,
    IAuditableEntity,
    ISoftDeletableEntity
{
    protected AuditableEntity()
    {
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedBy { get; set; }
}
