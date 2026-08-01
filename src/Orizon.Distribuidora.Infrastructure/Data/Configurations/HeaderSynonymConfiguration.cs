using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class HeaderSynonymConfiguration : IEntityTypeConfiguration<HeaderSynonym>
{
    public void Configure(EntityTypeBuilder<HeaderSynonym> builder)
    {
        builder.ToTable("HeaderSynonyms");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.CampoDestino).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Sinonimo).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Origem).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Prioridade).IsRequired();
        builder.Property(entity => entity.Ativo).IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedAt);
        builder.HasOne<Company>().WithMany().HasForeignKey(entity => entity.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.CompanyId, entity.Ativo, entity.CampoDestino, entity.Prioridade });
        builder.HasIndex(entity => new { entity.CompanyId, entity.CampoDestino, entity.Sinonimo });
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
