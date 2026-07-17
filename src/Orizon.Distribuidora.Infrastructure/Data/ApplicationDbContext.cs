using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Interfaces;
using Orizon.Distribuidora.Infrastructure.Identity;

namespace Orizon.Distribuidora.Infrastructure.Data;

public sealed class ApplicationDbContext
    : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Subcategory> Subcategories => Set<Subcategory>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();

    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<CommercialPartner> CommercialPartners => Set<CommercialPartner>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    public DbSet<InternalLocation> InternalLocations => Set<InternalLocation>();

    public DbSet<RegistrationStatus> RegistrationStatuses => Set<RegistrationStatus>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductChangeHistory> ProductChangeHistories => Set<ProductChangeHistory>();

    public DbSet<ImportacaoHistorico> ImportacoesHistorico => Set<ImportacaoHistorico>();

    public DbSet<ImportacaoItem> ImportacaoItens => Set<ImportacaoItem>();

    public DbSet<ImportacaoErro> ImportacaoErros => Set<ImportacaoErro>();

    public DbSet<ModeloImportacao> ModelosImportacao => Set<ModeloImportacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        ApplySoftDelete();

        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        ApplySoftDelete();

        return base.SaveChanges();
    }

    private void ApplyAuditInformation()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                continue;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;

                entry.Property(entity => entity.CreatedAt)
                    .IsModified = false;

                entry.Property(entity => entity.CreatedBy)
                    .IsModified = false;
            }
        }
    }

    private void ApplySoftDelete()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
        }
    }
}
