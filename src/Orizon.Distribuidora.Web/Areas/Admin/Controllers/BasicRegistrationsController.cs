using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.BasicRegistrations;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
[Route("Admin/Cadastros")]
public sealed class BasicRegistrationsController : Controller
{
    private const int PageSize = 10;

    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentCompanyAccessor currentCompanyAccessor;

    public BasicRegistrationsController(
        ApplicationDbContext dbContext,
        ICurrentCompanyAccessor currentCompanyAccessor)
    {
        this.dbContext = dbContext;
        this.currentCompanyAccessor = currentCompanyAccessor;
    }

    [HttpGet("{moduleKey}")]
    public async Task<IActionResult> Index(
        string moduleKey,
        string? search,
        bool? isActive,
        int? type,
        Guid? categoryId,
        Guid? warehouseId,
        bool? isSystem,
        int page = 1)
    {
        var module = BasicRegistrationModule.Resolve(moduleKey);
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        page = Math.Max(1, page);

        var query = BuildRowsQuery(
            module.Key,
            companyId,
            search,
            isActive,
            type,
            categoryId,
            warehouseId,
            isSystem);

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(item => item.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var model = new BasicRegistrationIndexViewModel
        {
            Module = module,
            Search = search,
            IsActive = isActive,
            Type = type,
            CategoryId = categoryId,
            WarehouseId = warehouseId,
            IsSystem = isSystem,
            Page = page,
            PageSize = PageSize,
            TotalItems = totalItems,
            Items = items,
            Categories = await GetCategoryOptionsAsync(companyId, categoryId),
            Warehouses = await GetWarehouseOptionsAsync(companyId, warehouseId)
        };

        return View("Index", model);
    }

    [HttpGet("{moduleKey}/novo")]
    public async Task<IActionResult> Create(string moduleKey)
    {
        var module = BasicRegistrationModule.Resolve(moduleKey);
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);

        var model = new BasicRegistrationFormViewModel
        {
            Module = module,
            ModuleKey = module.Key,
            Type = PersonType.Company,
            IsActive = true,
            Categories = await GetCategoryOptionsAsync(companyId, null),
            Warehouses = await GetWarehouseOptionsAsync(companyId, null)
        };

        return View("Form", model);
    }

    [HttpPost("{moduleKey}/novo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string moduleKey,
        BasicRegistrationFormViewModel model)
    {
        var module = BasicRegistrationModule.Resolve(moduleKey);
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);

        model.Module = module;
        model.ModuleKey = module.Key;
        await ValidateRelationshipsAndDuplicatesAsync(companyId, module, model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, companyId);
            return View("Form", model);
        }

        try
        {
            await AddEntityAsync(companyId, module, model);
            await dbContext.SaveChangesAsync();

            TempData["Success"] = $"{module.SingularTitle} criado com sucesso.";
            return RedirectToAction(nameof(Index), new { moduleKey = module.Key });
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, BuildFriendlyError(exception));
            await PopulateOptionsAsync(model, companyId);
            return View("Form", model);
        }
    }

    [HttpGet("{moduleKey}/{id:guid}/editar")]
    public async Task<IActionResult> Edit(
        string moduleKey,
        Guid id)
    {
        var module = BasicRegistrationModule.Resolve(moduleKey);
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var model = await BuildEditModelAsync(companyId, module, id);

        if (model is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model, companyId);

        return View("Form", model);
    }

    [HttpPost("{moduleKey}/{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string moduleKey,
        Guid id,
        BasicRegistrationFormViewModel model)
    {
        var module = BasicRegistrationModule.Resolve(moduleKey);
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);

        model.Id = id;
        model.Module = module;
        model.ModuleKey = module.Key;
        await ValidateRelationshipsAndDuplicatesAsync(companyId, module, model);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, companyId);
            return View("Form", model);
        }

        try
        {
            var updated = await UpdateEntityAsync(companyId, module, id, model);

            if (!updated)
            {
                return NotFound();
            }

            await dbContext.SaveChangesAsync();
            TempData["Success"] = $"{module.SingularTitle} atualizado com sucesso.";
            return RedirectToAction(nameof(Index), new { moduleKey = module.Key });
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, BuildFriendlyError(exception));
            await PopulateOptionsAsync(model, companyId);
            return View("Form", model);
        }
    }

    [HttpPost("{moduleKey}/{id:guid}/ativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(
        string moduleKey,
        Guid id)
    {
        return await ChangeActiveStateAsync(moduleKey, id, activate: true);
    }

    [HttpPost("{moduleKey}/{id:guid}/desativar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(
        string moduleKey,
        Guid id)
    {
        return await ChangeActiveStateAsync(moduleKey, id, activate: false);
    }

    [HttpPost("{moduleKey}/{id:guid}/excluir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        string moduleKey,
        Guid id)
    {
        var module = BasicRegistrationModule.Resolve(moduleKey);
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var deleted = await DeleteEntityAsync(companyId, module, id);

        if (!deleted)
        {
            return NotFound();
        }

        await dbContext.SaveChangesAsync();
        TempData["Success"] = $"{module.SingularTitle} excluído com sucesso.";

        return RedirectToAction(nameof(Index), new { moduleKey = module.Key });
    }

    private IQueryable<BasicRegistrationRowViewModel> BuildRowsQuery(
        string moduleKey,
        Guid companyId,
        string? search,
        bool? isActive,
        int? type,
        Guid? categoryId,
        Guid? warehouseId,
        bool? isSystem)
    {
        return moduleKey switch
        {
            "categorias" => ApplyBasicFilters(dbContext.Categories.AsNoTracking(), companyId, search, isActive)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Code,
                    Secondary = entity.Description,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "subcategorias" => ApplyBasicFilters(dbContext.Subcategories.AsNoTracking(), companyId, search, isActive)
                .Where(entity => categoryId == null || entity.CategoryId == categoryId)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Code,
                    Secondary = entity.Description,
                    RelatedName = entity.Category!.Name,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "marcas" => ApplyBasicFilters(dbContext.Brands.AsNoTracking(), companyId, search, isActive)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Code,
                    Secondary = entity.Description,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "unidades-medida" => ApplyBasicFilters(dbContext.UnitsOfMeasure.AsNoTracking(), companyId, search, isActive)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Abbreviation,
                    Secondary = entity.AllowsFraction ? "Permite fração" : "Inteiro",
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "grupos-produtos" => ApplyBasicFilters(dbContext.ProductGroups.AsNoTracking(), companyId, search, isActive)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Code,
                    Secondary = entity.Description,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "fornecedores" => ApplySupplierFilters(companyId, search, isActive, type)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.LegalName,
                    Code = entity.TradeName,
                    Secondary = entity.Email,
                    Document = entity.Document,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "parceiros-comerciais" => ApplyPartnerFilters(companyId, search, isActive, type)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.CommissionPercentage.ToString("0.##") + "%",
                    Secondary = entity.Email,
                    Document = entity.Document,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "depositos" => ApplyBasicFilters(dbContext.Warehouses.AsNoTracking(), companyId, search, isActive)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Code,
                    Secondary = entity.IsDefault ? "Depósito padrão" : entity.Description,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "localizacoes-internas" => ApplyBasicFilters(dbContext.InternalLocations.AsNoTracking(), companyId, search, isActive)
                .Where(entity => warehouseId == null || entity.WarehouseId == warehouseId)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Code,
                    Secondary = entity.Description,
                    RelatedName = entity.Warehouse!.Name,
                    IsActive = entity.IsActive,
                    CreatedAt = entity.CreatedAt
                }),
            "status-cadastro" => ApplyBasicFilters(dbContext.RegistrationStatuses.AsNoTracking(), companyId, search, isActive)
                .Where(entity => isSystem == null || entity.IsSystem == isSystem)
                .Select(entity => new BasicRegistrationRowViewModel
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Code = entity.Code,
                    Secondary = entity.Color,
                    IsActive = entity.IsActive,
                    IsSystem = entity.IsSystem,
                    CreatedAt = entity.CreatedAt
                }),
            _ => throw new InvalidOperationException("Cadastro não encontrado.")
        };
    }

    private static IQueryable<TEntity> ApplyBasicFilters<TEntity>(
        IQueryable<TEntity> query,
        Guid companyId,
        string? search,
        bool? isActive)
        where TEntity : BasicRegistrationEntity
    {
        query = query.Where(entity => entity.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(entity =>
                entity.Name.Contains(normalized) ||
                (entity.Code != null && entity.Code.Contains(normalized)) ||
                (entity.Description != null && entity.Description.Contains(normalized)));
        }

        if (isActive is not null)
        {
            query = query.Where(entity => entity.IsActive == isActive);
        }

        return query;
    }

    private IQueryable<Supplier> ApplySupplierFilters(
        Guid companyId,
        string? search,
        bool? isActive,
        int? type)
    {
        var query = dbContext.Suppliers
            .AsNoTracking()
            .Where(entity => entity.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(entity =>
                entity.LegalName.Contains(normalized) ||
                (entity.TradeName != null && entity.TradeName.Contains(normalized)) ||
                (entity.Document != null && entity.Document.Contains(normalized)) ||
                (entity.Email != null && entity.Email.Contains(normalized)));
        }

        if (isActive is not null)
        {
            query = query.Where(entity => entity.IsActive == isActive);
        }

        if (type is not null)
        {
            query = query.Where(entity => (int)entity.Type == type);
        }

        return query;
    }

    private IQueryable<CommercialPartner> ApplyPartnerFilters(
        Guid companyId,
        string? search,
        bool? isActive,
        int? type)
    {
        var query = dbContext.CommercialPartners
            .AsNoTracking()
            .Where(entity => entity.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(entity =>
                entity.Name.Contains(normalized) ||
                (entity.Document != null && entity.Document.Contains(normalized)) ||
                (entity.Email != null && entity.Email.Contains(normalized)));
        }

        if (isActive is not null)
        {
            query = query.Where(entity => entity.IsActive == isActive);
        }

        if (type is not null)
        {
            query = query.Where(entity => (int)entity.Type == type);
        }

        return query;
    }

    private async Task ValidateRelationshipsAndDuplicatesAsync(
        Guid companyId,
        BasicRegistrationModule module,
        BasicRegistrationFormViewModel model)
    {
        if (module.HasCategory)
        {
            if (model.CategoryId is null ||
                !await dbContext.Categories.AnyAsync(entity =>
                    entity.Id == model.CategoryId &&
                    entity.CompanyId == companyId))
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Selecione uma categoria válida.");
            }
        }

        if (module.HasWarehouse)
        {
            if (model.WarehouseId is null ||
                !await dbContext.Warehouses.AnyAsync(entity =>
                    entity.Id == model.WarehouseId &&
                    entity.CompanyId == companyId))
            {
                ModelState.AddModelError(nameof(model.WarehouseId), "Selecione um depósito válido.");
            }
        }

        if (module.HasUnitFields &&
            string.IsNullOrWhiteSpace(model.Abbreviation))
        {
            ModelState.AddModelError(nameof(model.Abbreviation), "A sigla é obrigatória.");
        }

        if (module.HasSupplierFields &&
            string.IsNullOrWhiteSpace(model.LegalName))
        {
            ModelState.AddModelError(nameof(model.LegalName), "A razão social/nome é obrigatória.");
        }

        if (module.HasPartnerFields &&
            string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "O nome é obrigatório.");
        }

        if (!module.HasSupplierFields &&
            !module.HasPartnerFields &&
            string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "O nome é obrigatório.");
        }

        if ((module.HasWarehouseFields || module.HasLocationFields || module.HasStatusFields) &&
            !module.HasUnitFields &&
            !module.HasSupplierFields &&
            !module.HasPartnerFields &&
            string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "O código é obrigatório.");
        }

        if (await HasDuplicateAsync(companyId, module, model))
        {
            ModelState.AddModelError(string.Empty, "Já existe um cadastro com os mesmos dados para esta empresa.");
        }
    }

    private async Task<bool> HasDuplicateAsync(
        Guid companyId,
        BasicRegistrationModule module,
        BasicRegistrationFormViewModel model)
    {
        var id = model.Id;
        var name = model.Name.Trim();
        var code = model.Code?.Trim().ToUpperInvariant();
        var abbreviation = model.Abbreviation?.Trim().ToUpperInvariant();
        var document = new string((model.Document ?? string.Empty).Where(char.IsDigit).ToArray());

        return module.Key switch
        {
            "categorias" => await dbContext.Categories.AnyAsync(entity => entity.CompanyId == companyId && entity.Name == name && entity.Id != id),
            "subcategorias" => await dbContext.Subcategories.AnyAsync(entity => entity.CompanyId == companyId && entity.CategoryId == model.CategoryId && entity.Name == name && entity.Id != id),
            "marcas" => await dbContext.Brands.AnyAsync(entity => entity.CompanyId == companyId && entity.Name == name && entity.Id != id),
            "unidades-medida" => await dbContext.UnitsOfMeasure.AnyAsync(entity => entity.CompanyId == companyId && entity.Abbreviation == abbreviation && entity.Id != id),
            "grupos-produtos" => await dbContext.ProductGroups.AnyAsync(entity => entity.CompanyId == companyId && entity.Name == name && entity.Id != id),
            "fornecedores" when !string.IsNullOrWhiteSpace(document) => await dbContext.Suppliers.AnyAsync(entity => entity.CompanyId == companyId && entity.Document == document && entity.Id != id),
            "parceiros-comerciais" when !string.IsNullOrWhiteSpace(document) => await dbContext.CommercialPartners.AnyAsync(entity => entity.CompanyId == companyId && entity.Document == document && entity.Id != id),
            "depositos" => await dbContext.Warehouses.AnyAsync(entity => entity.CompanyId == companyId && entity.Code == code && entity.Id != id),
            "localizacoes-internas" => await dbContext.InternalLocations.AnyAsync(entity => entity.CompanyId == companyId && entity.WarehouseId == model.WarehouseId && entity.Code == code && entity.Id != id),
            "status-cadastro" => await dbContext.RegistrationStatuses.AnyAsync(entity => entity.CompanyId == companyId && entity.Code == code && entity.Id != id),
            _ => false
        };
    }

    private async Task AddEntityAsync(
        Guid companyId,
        BasicRegistrationModule module,
        BasicRegistrationFormViewModel model)
    {
        switch (module.Key)
        {
            case "categorias":
                dbContext.Categories.Add(new Category(companyId, model.Name, model.Code, model.Description));
                break;
            case "subcategorias":
                dbContext.Subcategories.Add(new Subcategory(companyId, model.CategoryId!.Value, model.Name, model.Code, model.Description));
                break;
            case "marcas":
                dbContext.Brands.Add(new Brand(companyId, model.Name, model.Code, model.Description));
                break;
            case "unidades-medida":
                dbContext.UnitsOfMeasure.Add(new UnitOfMeasure(companyId, model.Name, model.Abbreviation!, model.Description, model.DecimalPlaces, model.AllowsFraction));
                break;
            case "grupos-produtos":
                dbContext.ProductGroups.Add(new ProductGroup(companyId, model.Name, model.Code, model.Description));
                break;
            case "fornecedores":
                var supplier = new Supplier(companyId, model.Type, model.LegalName!, model.TradeName, model.Document);
                supplier.Update(model.Type, model.LegalName!, model.TradeName, model.Document, model.StateRegistration, model.Email, model.Phone, model.MobilePhone, model.ContactName, model.ZipCode, model.Street, model.Number, model.Complement, model.Neighborhood, model.City, model.State, model.Notes);
                dbContext.Suppliers.Add(supplier);
                break;
            case "parceiros-comerciais":
                var partner = new CommercialPartner(companyId, model.Type, model.Name, model.Document, model.CommissionPercentage);
                partner.Update(model.Type, model.Name, model.Document, model.Email, model.Phone, model.ContactName, model.CommissionPercentage, model.Notes);
                dbContext.CommercialPartners.Add(partner);
                break;
            case "depositos":
                if (model.IsDefault)
                {
                    await ClearDefaultWarehouseAsync(companyId, null);
                }

                dbContext.Warehouses.Add(new Warehouse(companyId, model.Name, model.Code!, model.Description, model.IsDefault));
                break;
            case "localizacoes-internas":
                var location = new InternalLocation(companyId, model.WarehouseId!.Value, model.Code!, model.Name, model.Description);
                location.Update(model.WarehouseId.Value, model.Code!, model.Name, model.Description, model.Aisle, model.Section, model.Shelf, model.Position);
                dbContext.InternalLocations.Add(location);
                break;
            case "status-cadastro":
                dbContext.RegistrationStatuses.Add(new RegistrationStatus(companyId, model.Name, model.Code!, model.Description, model.Color, model.SortOrder, isSystem: false));
                break;
        }
    }

    private async Task<bool> UpdateEntityAsync(
        Guid companyId,
        BasicRegistrationModule module,
        Guid id,
        BasicRegistrationFormViewModel model)
    {
        switch (module.Key)
        {
            case "categorias":
                var category = await dbContext.Categories.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (category is null) return false;
                category.Update(model.Name, model.Code, model.Description);
                return true;
            case "subcategorias":
                var subcategory = await dbContext.Subcategories.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (subcategory is null) return false;
                subcategory.Update(model.CategoryId!.Value, model.Name, model.Code, model.Description);
                return true;
            case "marcas":
                var brand = await dbContext.Brands.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (brand is null) return false;
                brand.Update(model.Name, model.Code, model.Description);
                return true;
            case "unidades-medida":
                var unit = await dbContext.UnitsOfMeasure.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (unit is null) return false;
                unit.Update(model.Name, model.Abbreviation!, model.Description, model.DecimalPlaces, model.AllowsFraction);
                return true;
            case "grupos-produtos":
                var group = await dbContext.ProductGroups.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (group is null) return false;
                group.Update(model.Name, model.Code, model.Description);
                return true;
            case "fornecedores":
                var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (supplier is null) return false;
                supplier.Update(model.Type, model.LegalName!, model.TradeName, model.Document, model.StateRegistration, model.Email, model.Phone, model.MobilePhone, model.ContactName, model.ZipCode, model.Street, model.Number, model.Complement, model.Neighborhood, model.City, model.State, model.Notes);
                return true;
            case "parceiros-comerciais":
                var partner = await dbContext.CommercialPartners.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (partner is null) return false;
                partner.Update(model.Type, model.Name, model.Document, model.Email, model.Phone, model.ContactName, model.CommissionPercentage, model.Notes);
                return true;
            case "depositos":
                var warehouse = await dbContext.Warehouses.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (warehouse is null) return false;
                if (model.IsDefault)
                {
                    await ClearDefaultWarehouseAsync(companyId, id);
                }

                warehouse.Update(model.Name, model.Code!, model.Description, model.IsDefault);
                return true;
            case "localizacoes-internas":
                var location = await dbContext.InternalLocations.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (location is null) return false;
                location.Update(model.WarehouseId!.Value, model.Code!, model.Name, model.Description, model.Aisle, model.Section, model.Shelf, model.Position);
                return true;
            case "status-cadastro":
                var status = await dbContext.RegistrationStatuses.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (status is null) return false;
                status.Update(model.Name, model.Code!, model.Description, model.Color, model.SortOrder);
                return true;
            default:
                return false;
        }
    }

    private async Task<IActionResult> ChangeActiveStateAsync(
        string moduleKey,
        Guid id,
        bool activate)
    {
        var module = BasicRegistrationModule.Resolve(moduleKey);
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var changed = await ChangeActiveStateEntityAsync(companyId, module, id, activate);

        if (!changed)
        {
            return NotFound();
        }

        await dbContext.SaveChangesAsync();
        TempData["Success"] = activate
            ? $"{module.SingularTitle} ativado com sucesso."
            : $"{module.SingularTitle} desativado com sucesso.";

        return RedirectToAction(nameof(Index), new { moduleKey = module.Key });
    }

    private async Task<bool> ChangeActiveStateEntityAsync(
        Guid companyId,
        BasicRegistrationModule module,
        Guid id,
        bool activate)
    {
        dynamic? entity = module.Key switch
        {
            "categorias" => await dbContext.Categories.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "subcategorias" => await dbContext.Subcategories.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "marcas" => await dbContext.Brands.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "unidades-medida" => await dbContext.UnitsOfMeasure.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "grupos-produtos" => await dbContext.ProductGroups.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "fornecedores" => await dbContext.Suppliers.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "parceiros-comerciais" => await dbContext.CommercialPartners.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "depositos" => await dbContext.Warehouses.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "localizacoes-internas" => await dbContext.InternalLocations.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "status-cadastro" => await dbContext.RegistrationStatuses.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            _ => null
        };

        if (entity is null)
        {
            return false;
        }

        if (activate)
        {
            entity.Activate();
        }
        else
        {
            entity.Deactivate();
        }

        return true;
    }

    private async Task<bool> DeleteEntityAsync(
        Guid companyId,
        BasicRegistrationModule module,
        Guid id)
    {
        dynamic? entity = module.Key switch
        {
            "categorias" => await dbContext.Categories.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "subcategorias" => await dbContext.Subcategories.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "marcas" => await dbContext.Brands.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "unidades-medida" => await dbContext.UnitsOfMeasure.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "grupos-produtos" => await dbContext.ProductGroups.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "fornecedores" => await dbContext.Suppliers.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "parceiros-comerciais" => await dbContext.CommercialPartners.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "depositos" => await dbContext.Warehouses.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "localizacoes-internas" => await dbContext.InternalLocations.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            "status-cadastro" => await dbContext.RegistrationStatuses.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId),
            _ => null
        };

        if (entity is null)
        {
            return false;
        }

        if (entity is RegistrationStatus status)
        {
            status.EnsureCanDelete();
        }

        dbContext.Remove(entity);
        return true;
    }

    private async Task<BasicRegistrationFormViewModel?> BuildEditModelAsync(
        Guid companyId,
        BasicRegistrationModule module,
        Guid id)
    {
        var model = new BasicRegistrationFormViewModel
        {
            Id = id,
            Module = module,
            ModuleKey = module.Key
        };

        switch (module.Key)
        {
            case "categorias":
                var category = await dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (category is null) return null;
                FillBasic(model, category);
                break;
            case "subcategorias":
                var subcategory = await dbContext.Subcategories.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (subcategory is null) return null;
                FillBasic(model, subcategory);
                model.CategoryId = subcategory.CategoryId;
                break;
            case "marcas":
                var brand = await dbContext.Brands.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (brand is null) return null;
                FillBasic(model, brand);
                break;
            case "unidades-medida":
                var unit = await dbContext.UnitsOfMeasure.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (unit is null) return null;
                FillBasic(model, unit);
                model.Abbreviation = unit.Abbreviation;
                model.DecimalPlaces = unit.DecimalPlaces;
                model.AllowsFraction = unit.AllowsFraction;
                break;
            case "grupos-produtos":
                var group = await dbContext.ProductGroups.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (group is null) return null;
                FillBasic(model, group);
                break;
            case "fornecedores":
                var supplier = await dbContext.Suppliers.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (supplier is null) return null;
                model.Name = supplier.LegalName;
                model.LegalName = supplier.LegalName;
                model.TradeName = supplier.TradeName;
                model.Type = supplier.Type;
                model.Document = supplier.Document;
                model.StateRegistration = supplier.StateRegistration;
                model.Email = supplier.Email;
                model.Phone = supplier.Phone;
                model.MobilePhone = supplier.MobilePhone;
                model.ContactName = supplier.ContactName;
                model.ZipCode = supplier.ZipCode;
                model.Street = supplier.Street;
                model.Number = supplier.Number;
                model.Complement = supplier.Complement;
                model.Neighborhood = supplier.Neighborhood;
                model.City = supplier.City;
                model.State = supplier.State;
                model.Notes = supplier.Notes;
                model.IsActive = supplier.IsActive;
                break;
            case "parceiros-comerciais":
                var partner = await dbContext.CommercialPartners.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (partner is null) return null;
                model.Name = partner.Name;
                model.Type = partner.Type;
                model.Document = partner.Document;
                model.Email = partner.Email;
                model.Phone = partner.Phone;
                model.ContactName = partner.ContactName;
                model.CommissionPercentage = partner.CommissionPercentage;
                model.Notes = partner.Notes;
                model.IsActive = partner.IsActive;
                break;
            case "depositos":
                var warehouse = await dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (warehouse is null) return null;
                FillBasic(model, warehouse);
                model.IsDefault = warehouse.IsDefault;
                break;
            case "localizacoes-internas":
                var location = await dbContext.InternalLocations.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (location is null) return null;
                FillBasic(model, location);
                model.WarehouseId = location.WarehouseId;
                model.Aisle = location.Aisle;
                model.Section = location.Section;
                model.Shelf = location.Shelf;
                model.Position = location.Position;
                break;
            case "status-cadastro":
                var status = await dbContext.RegistrationStatuses.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);
                if (status is null) return null;
                FillBasic(model, status);
                model.Color = status.Color;
                model.SortOrder = status.SortOrder;
                model.IsSystem = status.IsSystem;
                break;
        }

        return model;
    }

    private static void FillBasic(
        BasicRegistrationFormViewModel model,
        BasicRegistrationEntity entity)
    {
        model.Name = entity.Name;
        model.Code = entity.Code;
        model.Description = entity.Description;
        model.IsActive = entity.IsActive;
    }

    private async Task ClearDefaultWarehouseAsync(
        Guid companyId,
        Guid? exceptId)
    {
        var currentDefaults = await dbContext.Warehouses
            .Where(entity =>
                entity.CompanyId == companyId &&
                entity.IsDefault &&
                (exceptId == null || entity.Id != exceptId))
            .ToListAsync();

        foreach (var warehouse in currentDefaults)
        {
            warehouse.UnmarkAsDefault();
        }
    }

    private async Task PopulateOptionsAsync(
        BasicRegistrationFormViewModel model,
        Guid companyId)
    {
        model.Categories = await GetCategoryOptionsAsync(companyId, model.CategoryId);
        model.Warehouses = await GetWarehouseOptionsAsync(companyId, model.WarehouseId);
    }

    private async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync(
        Guid companyId,
        Guid? selectedId)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(entity => entity.CompanyId == companyId && entity.IsActive)
            .OrderBy(entity => entity.Name)
            .Select(entity => new SelectListItem(
                entity.Name,
                entity.Id.ToString(),
                selectedId == entity.Id))
            .ToListAsync();
    }

    private async Task<IReadOnlyList<SelectListItem>> GetWarehouseOptionsAsync(
        Guid companyId,
        Guid? selectedId)
    {
        return await dbContext.Warehouses
            .AsNoTracking()
            .Where(entity => entity.CompanyId == companyId && entity.IsActive)
            .OrderBy(entity => entity.Name)
            .Select(entity => new SelectListItem(
                entity.Name,
                entity.Id.ToString(),
                selectedId == entity.Id))
            .ToListAsync();
    }

    private static string BuildFriendlyError(Exception exception)
    {
        if (exception is DbUpdateException)
        {
            return "Não foi possível salvar. Verifique se não há duplicidade para esta empresa.";
        }

        return exception.Message;
    }
}
