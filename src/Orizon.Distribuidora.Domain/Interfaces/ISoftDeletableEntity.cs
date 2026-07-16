namespace Orizon.Distribuidora.Domain.Interfaces;

public interface ISoftDeletableEntity
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedAt { get; set; }

    Guid? DeletedBy { get; set; }
}
