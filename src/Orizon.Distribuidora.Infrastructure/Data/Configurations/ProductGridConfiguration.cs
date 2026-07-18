using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class ProductGridPreferenceConfiguration : IEntityTypeConfiguration<ProductGridPreference>
{
    public void Configure(EntityTypeBuilder<ProductGridPreference> builder)
    {
        builder.ToTable("ProductGridPreferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StateJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class ProductSavedFilterConfiguration : IEntityTypeConfiguration<ProductSavedFilter>
{
    public void Configure(EntityTypeBuilder<ProductSavedFilter> builder)
    {
        builder.ToTable("ProductSavedFilters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.FilterJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.UserId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
