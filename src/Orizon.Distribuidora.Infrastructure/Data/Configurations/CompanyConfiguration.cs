using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.LegalName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.TradeName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(company => company.Document)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(company => company.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(company => company.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(company => company.CreatedAt)
            .IsRequired();

        builder.Property(company => company.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(company => company.Document)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(company => company.Slug)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasQueryFilter(company => !company.IsDeleted);
    }
}
