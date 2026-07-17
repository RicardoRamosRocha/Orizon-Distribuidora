using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId)
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(entity => entity.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.LegalName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.TradeName)
            .HasMaxLength(150);

        builder.Property(entity => entity.Document)
            .HasMaxLength(14);

        builder.Property(entity => entity.StateRegistration)
            .HasMaxLength(30);

        builder.Property(entity => entity.Email)
            .HasMaxLength(150);

        builder.Property(entity => entity.Phone)
            .HasMaxLength(30);

        builder.Property(entity => entity.MobilePhone)
            .HasMaxLength(30);

        builder.Property(entity => entity.ContactName)
            .HasMaxLength(150);

        builder.Property(entity => entity.ZipCode)
            .HasMaxLength(20);

        builder.Property(entity => entity.Street)
            .HasMaxLength(200);

        builder.Property(entity => entity.Number)
            .HasMaxLength(20);

        builder.Property(entity => entity.Complement)
            .HasMaxLength(100);

        builder.Property(entity => entity.Neighborhood)
            .HasMaxLength(100);

        builder.Property(entity => entity.City)
            .HasMaxLength(100);

        builder.Property(entity => entity.State)
            .HasMaxLength(2);

        builder.Property(entity => entity.Notes)
            .HasMaxLength(1000);

        builder.Property(entity => entity.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(entity => entity.CompanyId);

        builder.HasIndex(entity => new { entity.CompanyId, entity.Document })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE AND \"Document\" IS NOT NULL");

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
