using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Identity.Seed;

public static class ProductSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        if (!environment.IsDevelopment())
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var company = await dbContext.Companies.FirstOrDefaultAsync(entity => entity.Slug == "orizon-distribuidora");

        if (company is null)
        {
            return;
        }

        var companyId = company.Id;
        var unit = await EnsureUnitAsync(dbContext, companyId);
        var category = await EnsureCategoryAsync(dbContext, companyId, "Materiais de construção", "MAT");
        var services = await EnsureCategoryAsync(dbContext, companyId, "Serviços", "SERV");
        var brand = await EnsureBrandAsync(dbContext, companyId, "Linha Orizon");
        var group = await EnsureGroupAsync(dbContext, companyId, "Materiais gerais");
        var supplier = await EnsureSupplierAsync(dbContext, companyId);
        var partner = await EnsurePartnerAsync(dbContext, companyId);
        var warehouse = await EnsureWarehouseAsync(dbContext, companyId);
        var location = await EnsureLocationAsync(dbContext, companyId, warehouse.Id);

        var products = BuildProducts(unit.Id, category.Id, services.Id, brand.Id, group.Id, supplier.Id, partner.Id, warehouse.Id, location.Id);

        foreach (var product in products)
        {
            var exists = await dbContext.Products.AnyAsync(entity =>
                entity.CompanyId == companyId &&
                entity.InternalCode == product.InternalCode);

            if (exists)
            {
                continue;
            }

            var entity = new Product(companyId, product.InternalCode, product.Name, product.UnitId, product.Type);
            entity.Update(
                product.InternalCode,
                product.Sku,
                null,
                product.Reference,
                product.Name,
                product.ShortDescription,
                product.Description,
                product.Type,
                product.ControlsStock,
                product.IsActive,
                product.CategoryId,
                null,
                product.BrandId,
                product.UnitId,
                product.GroupId,
                product.SupplierId,
                product.PartnerId,
                product.ControlsStock ? product.WarehouseId : null,
                product.ControlsStock ? product.LocationId : null,
                product.Ncm,
                null,
                product.Cost,
                product.Price,
                product.Commission,
                product.CommissionValue,
                product.ValidUntil,
                product.ControlsStock ? product.MinimumStock : null,
                product.Notes);

            dbContext.Products.Add(entity);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<UnitOfMeasure> EnsureUnitAsync(ApplicationDbContext dbContext, Guid companyId)
    {
        var unit = await dbContext.UnitsOfMeasure.FirstOrDefaultAsync(entity => entity.CompanyId == companyId && entity.Abbreviation == "UN");
        if (unit is not null) return unit;
        unit = new UnitOfMeasure(companyId, "Unidade", "UN", null, 0, false);
        dbContext.UnitsOfMeasure.Add(unit);
        await dbContext.SaveChangesAsync();
        return unit;
    }

    private static async Task<Category> EnsureCategoryAsync(ApplicationDbContext dbContext, Guid companyId, string name, string code)
    {
        var entity = await dbContext.Categories.FirstOrDefaultAsync(item => item.CompanyId == companyId && item.Name == name);
        if (entity is not null) return entity;
        entity = new Category(companyId, name, code, null);
        dbContext.Categories.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private static async Task<Brand> EnsureBrandAsync(ApplicationDbContext dbContext, Guid companyId, string name)
    {
        var entity = await dbContext.Brands.FirstOrDefaultAsync(item => item.CompanyId == companyId && item.Name == name);
        if (entity is not null) return entity;
        entity = new Brand(companyId, name, "ORZ", null);
        dbContext.Brands.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private static async Task<ProductGroup> EnsureGroupAsync(ApplicationDbContext dbContext, Guid companyId, string name)
    {
        var entity = await dbContext.ProductGroups.FirstOrDefaultAsync(item => item.CompanyId == companyId && item.Name == name);
        if (entity is not null) return entity;
        entity = new ProductGroup(companyId, name, "GERAL", null);
        dbContext.ProductGroups.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private static async Task<Supplier> EnsureSupplierAsync(ApplicationDbContext dbContext, Guid companyId)
    {
        var entity = await dbContext.Suppliers.FirstOrDefaultAsync(item => item.CompanyId == companyId && item.LegalName == "Fornecedor Demonstrativo Orizon");
        if (entity is not null) return entity;
        entity = new Supplier(companyId, PersonType.Company, "Fornecedor Demonstrativo Orizon", "Fornecedor Orizon", null);
        dbContext.Suppliers.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private static async Task<CommercialPartner> EnsurePartnerAsync(ApplicationDbContext dbContext, Guid companyId)
    {
        var entity = await dbContext.CommercialPartners.FirstOrDefaultAsync(item => item.CompanyId == companyId && item.Name == "Parceiro Comercial Demonstrativo");
        if (entity is not null) return entity;
        entity = new CommercialPartner(companyId, PersonType.Company, "Parceiro Comercial Demonstrativo", null, 5);
        dbContext.CommercialPartners.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private static async Task<Warehouse> EnsureWarehouseAsync(ApplicationDbContext dbContext, Guid companyId)
    {
        var entity = await dbContext.Warehouses.FirstOrDefaultAsync(item => item.CompanyId == companyId && item.Code == "CD-01");
        if (entity is not null) return entity;
        entity = new Warehouse(companyId, "Centro de distribuição principal", "CD-01", null, true);
        dbContext.Warehouses.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private static async Task<InternalLocation> EnsureLocationAsync(ApplicationDbContext dbContext, Guid companyId, Guid warehouseId)
    {
        var entity = await dbContext.InternalLocations.FirstOrDefaultAsync(item => item.CompanyId == companyId && item.WarehouseId == warehouseId && item.Code == "A1-01");
        if (entity is not null) return entity;
        entity = new InternalLocation(companyId, warehouseId, "A1-01", "Rua A - Posição 01", null);
        dbContext.InternalLocations.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private static IEnumerable<ProductSeedItem> BuildProducts(
        Guid unitId,
        Guid categoryId,
        Guid servicesCategoryId,
        Guid brandId,
        Guid groupId,
        Guid supplierId,
        Guid partnerId,
        Guid warehouseId,
        Guid locationId)
    {
        var names = new[]
        {
            "Cimento CP II 50 kg", "Argamassa AC-I interna", "Argamassa AC-II externa", "Rejunte flexível branco",
            "Tinta acrílica fosca 18 L", "Selador acrílico 18 L", "Massa corrida 25 kg", "Rolo de pintura lã média",
            "Trena profissional 5 m", "Alicate universal isolado", "Chave Philips cabo emborrachado", "Disjuntor bipolar 32 A",
            "Cabo flexível 2,5 mm", "Tomada modular 10 A", "Curva PVC soldável 25 mm", "Tubo PVC soldável 25 mm",
            "Registro esfera PVC", "Parafuso cabeça chata", "Dobradiça reforçada", "Fechadura interna",
            "Piso cerâmico acetinado", "Revestimento parede branco", "Porcelanato técnico cinza", "Espaçador nivelador",
            "Impermeabilizante flexível", "Manta líquida branca", "Luva de segurança", "Óculos de proteção",
            "Broca widea 8 mm", "Serra copo 60 mm", "Kit hidráulico sob encomenda", "Bancada sob medida",
            "Porta técnica sob encomenda", "Montagem assistida de mostruário", "Serviço de corte especial",
            "Consultoria técnica em obra", "Produto intermediado aço especial", "Produto de terceiro luminária",
            "Produto de terceiro acabamento premium", "Produto de terceiro ferragem especial",
            "Cuba sob encomenda", "Quadro elétrico montado", "Tinta epóxi industrial", "Adesivo estrutural"
        };

        for (var index = 0; index < names.Length; index++)
        {
            var type = index switch
            {
                >= 33 and <= 35 => ProductType.Service,
                >= 36 and <= 39 => ProductType.ThirdParty,
                >= 30 and <= 32 or 40 or 41 => ProductType.MadeToOrder,
                _ => ProductType.Own
            };

            var controlsStock = type == ProductType.Own || (type == ProductType.MadeToOrder && index % 2 == 0);

            yield return new ProductSeedItem(
                $"PRD-{index + 1:0000}",
                $"SKU-{index + 1:0000}",
                $"REF-{index + 1:0000}",
                names[index],
                $"Item demonstrativo para operação comercial: {names[index]}",
                "Produto criado pelo seeder de Development da Sprint 2.",
                type,
                controlsStock,
                index % 13 != 0,
                type == ProductType.Service ? servicesCategoryId : categoryId,
                brandId,
                groupId,
                unitId,
                type == ProductType.ThirdParty || type == ProductType.Service ? null : supplierId,
                type == ProductType.ThirdParty || index % 5 == 0 ? partnerId : null,
                warehouseId,
                locationId,
                18 + index,
                31 + (index * 1.7m),
                index % 4 == 0 ? CommissionType.Percentage : null,
                index % 4 == 0 ? 4.5m : null,
                index % 6 == 0 ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45 + index)),
                controlsStock ? 5 + index : null,
                "Sem saldo físico demonstrativo nesta sprint.",
                "12345678");
        }
    }

    private sealed record ProductSeedItem(
        string InternalCode,
        string Sku,
        string Reference,
        string Name,
        string ShortDescription,
        string Description,
        ProductType Type,
        bool ControlsStock,
        bool IsActive,
        Guid CategoryId,
        Guid BrandId,
        Guid GroupId,
        Guid UnitId,
        Guid? SupplierId,
        Guid? PartnerId,
        Guid WarehouseId,
        Guid LocationId,
        decimal Cost,
        decimal Price,
        CommissionType? Commission,
        decimal? CommissionValue,
        DateOnly? ValidUntil,
        decimal? MinimumStock,
        string Notes,
        string Ncm);
}
