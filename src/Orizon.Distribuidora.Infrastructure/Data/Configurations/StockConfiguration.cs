using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances", table =>
        {
            table.HasCheckConstraint("CK_StockBalances_QuantityOnHand_NonNegative", "\"QuantityOnHand\" >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();
        builder.Property(x => x.QuantityOnHand).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Movements).WithOne(x => x.StockBalance).HasForeignKey(x => x.StockBalanceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CompanyId, x.ProductId, x.WarehouseId }).IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
        {
            table.HasCheckConstraint("CK_StockMovements_Quantity_Positive", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_StockMovements_ResultingQuantity_NonNegative", "\"ResultingQuantity\" >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.Direction).HasConversion<int>().IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.PreviousQuantity).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.ResultingQuantity).HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.UnitCost).HasPrecision(18, 6);
        builder.Ignore(x => x.TotalCost);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ReferenceType).HasMaxLength(100);
        builder.Property(x => x.ReferenceId).HasMaxLength(150);
        builder.Property(x => x.DocumentNumber).HasMaxLength(100);
        builder.Property(x => x.OperationKey).HasMaxLength(150);
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.InternalLocation).WithMany().HasForeignKey(x => x.InternalLocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.CompanyId, x.ProductId, x.OccurredAt });
        builder.HasIndex(x => new { x.CompanyId, x.WarehouseId, x.OccurredAt });
        builder.HasIndex(x => new { x.CompanyId, x.ReferenceType, x.ReferenceId });
        builder.HasIndex(x => new { x.CompanyId, x.OperationKey }).IsUnique()
            .HasFilter("\"OperationKey\" IS NOT NULL");
    }
}
