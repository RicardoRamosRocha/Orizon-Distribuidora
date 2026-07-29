using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> b)
    {
        b.ToTable("Quotes", t => t.HasCheckConstraint("CK_Quotes_Total_NonNegative", "\"Total\" >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.CompanyId).IsRequired(); b.Property(x => x.Number).IsRequired();
        b.Ignore(x => x.DisplayNumber);
        b.Property(x => x.CustomerName).HasMaxLength(150).IsRequired(); b.Property(x => x.CustomerDocument).HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<int>().IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000); b.Property(x => x.DeliveryAddress).HasMaxLength(1000);
        Money(b.Property(x => x.Subtotal)); Money(b.Property(x => x.Discount)); Money(b.Property(x => x.Freight));
        Money(b.Property(x => x.AdditionalCharges)); Money(b.Property(x => x.Total));
        b.Property(x => x.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PriceTable).WithMany().HasForeignKey(x => x.PriceTableId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Items).WithOne(x => x.Quote).HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.Number }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        b.HasIndex(x => new { x.CompanyId, x.Status, x.IssuedAt });
        b.HasIndex(x => new { x.CompanyId, x.SaleId }).IsUnique().HasFilter("\"SaleId\" IS NOT NULL");
        b.HasQueryFilter(x => !x.IsDeleted);
    }
    private static void Money(PropertyBuilder<decimal> p) => p.HasPrecision(18, 2).IsRequired();
}

public sealed class QuoteItemConfiguration : IEntityTypeConfiguration<QuoteItem>
{
    public void Configure(EntityTypeBuilder<QuoteItem> b)
    {
        b.ToTable("QuoteItems", t => { t.HasCheckConstraint("CK_QuoteItems_Quantity_Positive", "\"Quantity\" > 0"); t.HasCheckConstraint("CK_QuoteItems_Total_NonNegative", "\"Total\" >= 0"); });
        b.HasKey(x => x.Id); b.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Description).HasMaxLength(250).IsRequired(); b.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        b.Property(x => x.Quantity).HasPrecision(18, 6); b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.Discount).HasPrecision(18, 2); b.Property(x => x.Total).HasPrecision(18, 2);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.QuoteId });
        b.HasQueryFilter(x => x.Quote != null && !x.Quote.IsDeleted);
    }
}

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> b)
    {
        b.ToTable("Sales", t => t.HasCheckConstraint("CK_Sales_Total_NonNegative", "\"Total\" >= 0"));
        b.HasKey(x => x.Id); b.Property(x => x.Number).IsRequired(); b.Ignore(x => x.DisplayNumber);
        b.Property(x => x.CustomerName).HasMaxLength(150).IsRequired(); b.Property(x => x.CustomerDocument).HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<int>(); b.Property(x => x.PaymentStatus).HasConversion<int>(); b.Property(x => x.FiscalStatus).HasConversion<int>();
        b.Property(x => x.FiscalExternalId).HasMaxLength(150); b.Property(x => x.FiscalAccessKey).HasMaxLength(80); b.Property(x => x.FiscalMessage).HasMaxLength(1000);
        b.Property(x => x.Notes).HasMaxLength(2000); b.Property(x => x.DeliveryAddress).HasMaxLength(1000);
        Money(b.Property(x => x.Subtotal)); Money(b.Property(x => x.Discount)); Money(b.Property(x => x.Freight));
        Money(b.Property(x => x.AdditionalCharges)); Money(b.Property(x => x.Total));
        b.Property(x => x.ConcurrencyToken).IsConcurrencyToken().IsRequired();
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Quote).WithOne().HasForeignKey<Sale>(x => x.QuoteId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Items).WithOne(x => x.Sale).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.CompanyId, x.Number }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        b.HasIndex(x => new { x.CompanyId, x.QuoteId }).IsUnique().HasFilter("\"QuoteId\" IS NOT NULL");
        b.HasIndex(x => new { x.CompanyId, x.Status, x.IssuedAt }); b.HasQueryFilter(x => !x.IsDeleted);
    }
    private static void Money(PropertyBuilder<decimal> p) => p.HasPrecision(18, 2).IsRequired();
}

public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> b)
    {
        b.ToTable("SaleItems", t => { t.HasCheckConstraint("CK_SaleItems_Quantity_Positive", "\"Quantity\" > 0"); t.HasCheckConstraint("CK_SaleItems_Total_NonNegative", "\"Total\" >= 0"); });
        b.HasKey(x => x.Id); b.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Description).HasMaxLength(250).IsRequired(); b.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        b.Property(x => x.Quantity).HasPrecision(18, 6); b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.Discount).HasPrecision(18, 2); b.Property(x => x.Total).HasPrecision(18, 2);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.SaleId });
        b.HasQueryFilter(x => x.Sale != null && !x.Sale.IsDeleted);
    }
}
