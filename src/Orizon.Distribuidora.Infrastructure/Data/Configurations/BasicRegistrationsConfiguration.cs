using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Infrastructure.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        ConfigureBasic(builder, "Categories");

        builder.HasIndex(entity => new { entity.CompanyId, entity.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }

    internal static void ConfigureBasic<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        string tableName)
        where TEntity : BasicRegistrationEntity
    {
        builder.ToTable(tableName);
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CompanyId)
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(entity => entity.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(entity => entity.Code)
            .HasMaxLength(50);

        builder.Property(entity => entity.Description)
            .HasMaxLength(500);

        builder.Property(entity => entity.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(entity => entity.CreatedAt)
            .IsRequired();

        builder.Property(entity => entity.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        CategoryConfiguration.ConfigureBasic(builder, "Brands");

        builder.HasIndex(entity => new { entity.CompanyId, entity.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}

public sealed class ProductGroupConfiguration : IEntityTypeConfiguration<ProductGroup>
{
    public void Configure(EntityTypeBuilder<ProductGroup> builder)
    {
        CategoryConfiguration.ConfigureBasic(builder, "ProductGroups");

        builder.HasIndex(entity => new { entity.CompanyId, entity.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}

public sealed class SubcategoryConfiguration : IEntityTypeConfiguration<Subcategory>
{
    public void Configure(EntityTypeBuilder<Subcategory> builder)
    {
        CategoryConfiguration.ConfigureBasic(builder, "Subcategories");

        builder.HasOne(entity => entity.Category)
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.CategoryId, entity.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}

public sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        CategoryConfiguration.ConfigureBasic(builder, "UnitsOfMeasure");

        builder.Property(entity => entity.Abbreviation)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(entity => entity.DecimalPlaces)
            .IsRequired();

        builder.Property(entity => entity.AllowsFraction)
            .IsRequired();

        builder.HasIndex(entity => new { entity.CompanyId, entity.Abbreviation })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        CategoryConfiguration.ConfigureBasic(builder, "Warehouses");

        builder.Property(entity => entity.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.IsDefault)
            .IsRequired();

        builder.HasIndex(entity => new { entity.CompanyId, entity.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        builder.HasIndex(entity => new { entity.CompanyId, entity.IsDefault })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE AND \"IsDefault\" = TRUE");
    }
}

public sealed class InternalLocationConfiguration : IEntityTypeConfiguration<InternalLocation>
{
    public void Configure(EntityTypeBuilder<InternalLocation> builder)
    {
        CategoryConfiguration.ConfigureBasic(builder, "InternalLocations");

        builder.Property(entity => entity.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.Aisle)
            .HasMaxLength(50);

        builder.Property(entity => entity.Section)
            .HasMaxLength(50);

        builder.Property(entity => entity.Shelf)
            .HasMaxLength(50);

        builder.Property(entity => entity.Position)
            .HasMaxLength(50);

        builder.HasOne(entity => entity.Warehouse)
            .WithMany(entity => entity.Locations)
            .HasForeignKey(entity => entity.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CompanyId, entity.WarehouseId, entity.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}

public sealed class RegistrationStatusConfiguration : IEntityTypeConfiguration<RegistrationStatus>
{
    public void Configure(EntityTypeBuilder<RegistrationStatus> builder)
    {
        CategoryConfiguration.ConfigureBasic(builder, "RegistrationStatuses");

        builder.Property(entity => entity.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.Color)
            .HasMaxLength(7);

        builder.Property(entity => entity.SortOrder)
            .IsRequired();

        builder.Property(entity => entity.IsSystem)
            .IsRequired();

        builder.HasIndex(entity => new { entity.CompanyId, entity.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
