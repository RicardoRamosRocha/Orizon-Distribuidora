using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Infrastructure.Identity;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class ApplicationRoleConfiguration
    : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(role => role.Description)
            .HasMaxLength(250);
    }
}
