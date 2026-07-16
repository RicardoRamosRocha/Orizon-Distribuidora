using Microsoft.AspNetCore.Identity;

namespace Orizon.Distribuidora.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
