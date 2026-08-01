using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendations");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Module).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Type).IsRequired();
        builder.Property(entity => entity.Severity).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.ReferenceId).HasMaxLength(200);
        builder.Property(entity => entity.ActionUrl).HasMaxLength(1000);
        builder.Property(entity => entity.MetadataJson).HasColumnType("jsonb");
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.HasOne<Company>().WithMany().HasForeignKey(entity => entity.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.CompanyId, entity.Module, entity.DismissedAt, entity.ExpiresAt });
        builder.HasIndex(entity => new { entity.CompanyId, entity.ReferenceId });
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
