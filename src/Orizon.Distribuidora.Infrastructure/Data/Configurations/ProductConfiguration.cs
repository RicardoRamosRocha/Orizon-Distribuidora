using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(entity => entity.InternalCode).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Sku).HasMaxLength(80);
        builder.Property(entity => entity.Barcode).HasMaxLength(32);
        builder.Property(entity => entity.Reference).HasMaxLength(100);
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ShortDescription).HasMaxLength(300);
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.ProductType).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.ControlsStock).IsRequired();
        builder.Property(entity => entity.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(entity => entity.Ncm).HasMaxLength(8);
        builder.Property(entity => entity.Cest).HasMaxLength(7);
        builder.Property(entity => entity.ImagePath).HasMaxLength(300);
        builder.Property(entity => entity.CostPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.SalePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.CommissionType).HasConversion<int>();
        builder.Property(entity => entity.CommissionValue).HasPrecision(18, 4);
        builder.Property(entity => entity.MinimumStock).HasPrecision(18, 4);
        builder.Property(entity => entity.Notes).HasMaxLength(2000);
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();

        builder.HasOne(entity => entity.Category)
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Subcategory)
            .WithMany()
            .HasForeignKey(entity => entity.SubcategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Brand)
            .WithMany()
            .HasForeignKey(entity => entity.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(entity => entity.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.ProductGroup)
            .WithMany()
            .HasForeignKey(entity => entity.ProductGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.MainSupplier)
            .WithMany()
            .HasForeignKey(entity => entity.MainSupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Partner)
            .WithMany()
            .HasForeignKey(entity => entity.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.DefaultWarehouse)
            .WithMany()
            .HasForeignKey(entity => entity.DefaultWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.DefaultWarehouseLocation)
            .WithMany()
            .HasForeignKey(entity => entity.DefaultWarehouseLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.InternalCode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(entity => new { entity.CompanyId, entity.Sku })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE AND \"Sku\" IS NOT NULL");

        builder.HasIndex(entity => new { entity.CompanyId, entity.Barcode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE AND \"Barcode\" IS NOT NULL");

        builder.HasIndex(entity => new { entity.CompanyId, entity.Name });
        builder.HasIndex(entity => new { entity.CompanyId, entity.IsActive });
        builder.HasIndex(entity => new { entity.CompanyId, entity.CategoryId });
        builder.HasIndex(entity => new { entity.CompanyId, entity.BrandId });
        builder.HasIndex(entity => new { entity.CompanyId, entity.MainSupplierId });
        builder.HasIndex(entity => new { entity.CompanyId, entity.PartnerId });
        builder.HasIndex(entity => new { entity.CompanyId, entity.ProductType });
        builder.HasIndex(entity => new { entity.CompanyId, entity.ControlsStock });

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

public sealed class ProductChangeHistoryConfiguration
    : IEntityTypeConfiguration<ProductChangeHistory>
{
    public void Configure(EntityTypeBuilder<ProductChangeHistory> builder)
    {
        builder.ToTable("ProductChangeHistories");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.Property(entity => entity.ProductId).IsRequired();
        builder.Property(entity => entity.FieldName).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.OldValue).HasMaxLength(2000);
        builder.Property(entity => entity.NewValue).HasMaxLength(2000);
        builder.Property(entity => entity.Origin).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Product)
            .WithMany()
            .HasForeignKey(entity => entity.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.ProductId, entity.CreatedAt });
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
