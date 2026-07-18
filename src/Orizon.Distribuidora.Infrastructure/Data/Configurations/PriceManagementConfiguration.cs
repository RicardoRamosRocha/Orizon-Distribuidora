using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class PriceTableConfiguration : IEntityTypeConfiguration<PriceTable>
{
    public void Configure(EntityTypeBuilder<PriceTable> builder)
    {
        builder.ToTable("PriceTables");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.Property(entity => entity.IsDefault).HasDefaultValue(false).IsRequired();
        builder.Property(entity => entity.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.ProductPrices)
            .WithOne(entity => entity.PriceTable)
            .HasForeignKey(entity => entity.PriceTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(entity => entity.IsDefault);
        builder.HasIndex(entity => new { entity.CompanyId, entity.IsDefault });
        builder.HasIndex(entity => new { entity.CompanyId, entity.IsActive });

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

public sealed class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("ProductPrices");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.Property(entity => entity.ProductId).IsRequired();
        builder.Property(entity => entity.PriceTableId).IsRequired();
        builder.Property(entity => entity.CostPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.SalePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.PromotionalPrice).HasPrecision(18, 2);
        builder.Property(entity => entity.MinimumMargin).HasPrecision(18, 4);
        builder.Property(entity => entity.CurrentMargin).HasPrecision(18, 4);
        builder.Property(entity => entity.Markup).HasPrecision(18, 4);
        builder.Property(entity => entity.IsPromotionActive).HasDefaultValue(false).IsRequired();
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

        builder.HasOne(entity => entity.PriceTable)
            .WithMany(entity => entity.ProductPrices)
            .HasForeignKey(entity => entity.PriceTableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.PriceHistories)
            .WithOne(entity => entity.ProductPrice)
            .HasForeignKey(entity => entity.ProductPriceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.ProductId);
        builder.HasIndex(entity => entity.PriceTableId);
        builder.HasIndex(entity => new { entity.CompanyId, entity.ProductId, entity.PriceTableId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(entity => new { entity.CompanyId, entity.IsPromotionActive });

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

public sealed class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistories");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId).IsRequired();
        builder.Property(entity => entity.ProductPriceId).IsRequired();
        builder.Property(entity => entity.PreviousCostPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.NewCostPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.PreviousSalePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.NewSalePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.PreviousMargin).HasPrecision(18, 4);
        builder.Property(entity => entity.NewMargin).HasPrecision(18, 4);
        builder.Property(entity => entity.DifferenceValue).HasPrecision(18, 2).IsRequired();
        builder.Property(entity => entity.DifferencePercent).HasPrecision(18, 4).IsRequired();
        builder.Property(entity => entity.ChangeReason).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.ChangedAt).IsRequired();
        builder.Property(entity => entity.Origin).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.ProductPrice)
            .WithMany(entity => entity.PriceHistories)
            .HasForeignKey(entity => entity.ProductPriceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.ChangedAt);
        builder.HasIndex(entity => new { entity.CompanyId, entity.ProductPriceId, entity.ChangedAt });

        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
