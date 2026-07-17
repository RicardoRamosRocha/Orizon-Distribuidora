using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Products;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
[Route("Admin/Products")]
public sealed class ProductsController : Controller
{
    private static readonly HashSet<string> AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxImageBytes = 2 * 1024 * 1024;

    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentCompanyAccessor currentCompanyAccessor;
    private readonly IWebHostEnvironment environment;

    public ProductsController(
        ApplicationDbContext dbContext,
        ICurrentCompanyAccessor currentCompanyAccessor,
        IWebHostEnvironment environment)
    {
        this.dbContext = dbContext;
        this.currentCompanyAccessor = currentCompanyAccessor;
        this.environment = environment;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(ProductFilterViewModel filter)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        NormalizeFilter(filter);

        var query = ApplyFilters(dbContext.Products.AsNoTracking(), companyId, filter);
        var totalItems = await query.CountAsync();
        var items = await ApplySorting(query, filter)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(product => new ProductRowViewModel
            {
                Id = product.Id,
                InternalCode = product.InternalCode,
                Sku = product.Sku,
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                ProductType = product.ProductType,
                ControlsStock = product.ControlsStock,
                CategoryName = product.Category != null ? product.Category.Name : null,
                BrandName = product.Brand != null ? product.Brand.Name : null,
                UnitName = product.UnitOfMeasure!.Abbreviation,
                CostPrice = product.CostPrice,
                SalePrice = product.SalePrice,
                MarginPercentage = product.SalePrice > 0 ? Math.Round(((product.SalePrice - product.CostPrice) / product.SalePrice) * 100, 2) : 0,
                PriceValidUntil = product.PriceValidUntil,
                MinimumStock = product.MinimumStock,
                IsActive = product.IsActive,
                ImagePath = product.ImagePath
            })
            .ToListAsync();

        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (decimal)filter.PageSize));

        var model = new ProductIndexViewModel
        {
            Filter = filter,
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            PageStart = totalItems == 0 ? 0 : ((filter.Page - 1) * filter.PageSize) + 1,
            PageEnd = Math.Min(totalItems, filter.Page * filter.PageSize),
            Categories = await GetCategoryOptionsAsync(companyId, filter.CategoryId),
            Brands = await GetBrandOptionsAsync(companyId, filter.BrandId),
            Suppliers = await GetSupplierOptionsAsync(companyId, filter.SupplierId),
            Partners = await GetPartnerOptionsAsync(companyId, filter.PartnerId)
        };

        return View(model);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var model = new ProductFormViewModel
        {
            IsActive = true,
            ControlsStock = true,
            CommissionType = CommissionType.Percentage
        };

        await PopulateOptionsAsync(model, companyId);
        return View("Form", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model, IFormFile? imageFile)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        await ValidateProductAsync(companyId, model, null);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, companyId);
            return View("Form", model);
        }

        try
        {
            var product = new Product(companyId, model.InternalCode, model.Name, model.UnitOfMeasureId!.Value, model.ProductType);
            ApplyModel(product, model);
            var imagePath = await SaveImageAsync(imageFile);
            product.SetImagePath(imagePath);
            dbContext.Products.Add(product);
            dbContext.ProductChangeHistories.Add(new ProductChangeHistory(companyId, product.Id, "Produto", null, "Criado", "cadastro"));
            await dbContext.SaveChangesAsync();

            TempData["Success"] = "Produto criado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, BuildFriendlyError(exception));
            await PopulateOptionsAsync(model, companyId);
            return View("Form", model);
        }
    }

    [HttpGet("{id:guid}/Edit")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var model = await BuildFormModelAsync(companyId, id);

        if (model is null)
        {
            return NotFound();
        }

        await PopulateOptionsAsync(model, companyId);
        return View("Form", model);
    }

    [HttpPost("{id:guid}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ProductFormViewModel model, IFormFile? imageFile)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        model.Id = id;
        await ValidateProductAsync(companyId, model, id);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model, companyId);
            return View("Form", model);
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);

        if (product is null)
        {
            return NotFound();
        }

        try
        {
            var before = Snapshot(product);
            ApplyModel(product, model);

            if (model.RemoveImage)
            {
                DeleteImage(product.ImagePath);
                product.SetImagePath(null);
            }

            var imagePath = await SaveImageAsync(imageFile);
            if (imagePath is not null)
            {
                DeleteImage(product.ImagePath);
                product.SetImagePath(imagePath);
            }

            AddHistory(companyId, product, before, "edição");
            await dbContext.SaveChangesAsync();

            TempData["Success"] = "Produto atualizado com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, BuildFriendlyError(exception));
            await PopulateOptionsAsync(model, companyId);
            return View("Form", model);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        return await Edit(id);
    }

    [HttpPost("{id:guid}/Activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        return await ChangeStatusAsync([id], true);
    }

    [HttpPost("{id:guid}/Deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        return await ChangeStatusAsync([id], false);
    }

    [HttpPost("BulkActivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkActivate(Guid[] selectedIds)
    {
        return await ChangeStatusAsync(selectedIds, true);
    }

    [HttpPost("BulkDeactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeactivate(Guid[] selectedIds)
    {
        return await ChangeStatusAsync(selectedIds, false);
    }

    [HttpPost("{id:guid}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var product = await dbContext.Products.FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);

        if (product is null)
        {
            return NotFound();
        }

        dbContext.Products.Remove(product);
        dbContext.ProductChangeHistories.Add(new ProductChangeHistory(companyId, id, "Produto", "Ativo", "Excluído", "exclusão"));
        await dbContext.SaveChangesAsync();
        TempData["Success"] = "Produto excluído com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}/History")]
    public async Task<IActionResult> History(Guid id)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id && entity.CompanyId == companyId);

        if (product is null)
        {
            return NotFound();
        }

        var model = new ProductHistoryViewModel
        {
            ProductName = product.Name,
            Items = await dbContext.ProductChangeHistories
                .AsNoTracking()
                .Where(entity => entity.CompanyId == companyId && entity.ProductId == id)
                .OrderByDescending(entity => entity.CreatedAt)
                .Select(entity => new ProductHistoryRowViewModel
                {
                    CreatedAt = entity.CreatedAt,
                    FieldName = entity.FieldName,
                    OldValue = entity.OldValue,
                    NewValue = entity.NewValue,
                    Origin = entity.Origin
                })
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet("Subcategories")]
    public async Task<IActionResult> Subcategories(Guid categoryId)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var exists = await dbContext.Categories.AnyAsync(entity => entity.Id == categoryId && entity.CompanyId == companyId);

        if (!exists)
        {
            return Json(Array.Empty<object>());
        }

        var items = await dbContext.Subcategories
            .AsNoTracking()
            .Where(entity => entity.CompanyId == companyId && entity.CategoryId == categoryId && entity.IsActive)
            .OrderBy(entity => entity.Name)
            .Select(entity => new { id = entity.Id, text = entity.Name })
            .ToListAsync();

        return Json(items);
    }

    [HttpGet("Locations")]
    public async Task<IActionResult> Locations(Guid warehouseId)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var exists = await dbContext.Warehouses.AnyAsync(entity => entity.Id == warehouseId && entity.CompanyId == companyId);

        if (!exists)
        {
            return Json(Array.Empty<object>());
        }

        var items = await dbContext.InternalLocations
            .AsNoTracking()
            .Where(entity => entity.CompanyId == companyId && entity.WarehouseId == warehouseId && entity.IsActive)
            .OrderBy(entity => entity.Name)
            .Select(entity => new { id = entity.Id, text = entity.Name })
            .ToListAsync();

        return Json(items);
    }

    [HttpPost("InlineEdit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InlineEdit([FromBody] ProductInlineEditRequest request)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var product = await dbContext.Products.FirstOrDefaultAsync(entity => entity.Id == request.Id && entity.CompanyId == companyId);

        if (product is null)
        {
            return NotFound(new { ok = false, message = "Produto não encontrado." });
        }

        try
        {
            var before = Snapshot(product);

            switch (request.Field)
            {
                case "cost":
                    product.UpdateInlinePrice(ParseDecimal(request.Value), null);
                    break;
                case "price":
                    product.UpdateInlinePrice(null, ParseDecimal(request.Value));
                    break;
                case "status":
                    if (bool.TryParse(request.Value, out var active) && active) product.Activate(); else product.Deactivate();
                    break;
                case "minimumStock":
                    product.UpdateInlineMinimumStock(ParseNullableDecimal(request.Value));
                    break;
                case "priceValidUntil":
                    product.UpdateInlinePriceValidity(ParseNullableDate(request.Value));
                    break;
                default:
                    return BadRequest(new { ok = false, message = "Campo não permitido para edição inline." });
            }

            AddHistory(companyId, product, before, "edição inline");
            await dbContext.SaveChangesAsync();

            return Json(new
            {
                ok = true,
                margin = product.CalculateMarginPercentage(),
                message = "Salvo."
            });
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException or FormatException or InvalidOperationException)
        {
            return BadRequest(new { ok = false, message = BuildFriendlyError(exception) });
        }
    }

    private static void NormalizeFilter(ProductFilterViewModel filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = filter.PageSize is 25 or 50 or 100 ? filter.PageSize : 50;
        filter.SortBy = NormalizeSortBy(filter.SortBy);
        filter.SortDirection = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
    }

    private static IQueryable<Product> ApplyFilters(
        IQueryable<Product> query,
        Guid companyId,
        ProductFilterViewModel filter)
    {
        query = query.Where(product => product.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(product =>
                product.InternalCode.Contains(search) ||
                (product.Sku != null && product.Sku.Contains(search)) ||
                (product.Barcode != null && product.Barcode.Contains(search)) ||
                (product.Reference != null && product.Reference.Contains(search)) ||
                product.Name.Contains(search) ||
                (product.ShortDescription != null && product.ShortDescription.Contains(search)) ||
                (product.Description != null && product.Description.Contains(search)));
        }

        if (filter.ProductType is not null) query = query.Where(product => product.ProductType == filter.ProductType);
        if (filter.IsActive is not null) query = query.Where(product => product.IsActive == filter.IsActive);
        if (filter.CategoryId is not null) query = query.Where(product => product.CategoryId == filter.CategoryId);
        if (filter.BrandId is not null) query = query.Where(product => product.BrandId == filter.BrandId);
        if (filter.SupplierId is not null) query = query.Where(product => product.MainSupplierId == filter.SupplierId);
        if (filter.PartnerId is not null) query = query.Where(product => product.PartnerId == filter.PartnerId);
        if (filter.ControlsStock is not null) query = query.Where(product => product.ControlsStock == filter.ControlsStock);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        query = filter.PriceStatus switch
        {
            ProductPriceStatus.Valid => query.Where(product => product.PriceValidUntil == null || product.PriceValidUntil >= today),
            ProductPriceStatus.Expired => query.Where(product => product.PriceValidUntil != null && product.PriceValidUntil < today),
            ProductPriceStatus.NoValidity => query.Where(product => product.PriceValidUntil == null),
            _ => query
        };

        return query;
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, ProductFilterViewModel filter)
    {
        var desc = filter.SortDirection == "desc";

        return filter.SortBy switch
        {
            "code" => desc ? query.OrderByDescending(product => product.InternalCode) : query.OrderBy(product => product.InternalCode),
            "category" => desc ? query.OrderByDescending(product => product.Category!.Name) : query.OrderBy(product => product.Category!.Name),
            "brand" => desc ? query.OrderByDescending(product => product.Brand!.Name) : query.OrderBy(product => product.Brand!.Name),
            "cost" => desc ? query.OrderByDescending(product => product.CostPrice) : query.OrderBy(product => product.CostPrice),
            "price" => desc ? query.OrderByDescending(product => product.SalePrice) : query.OrderBy(product => product.SalePrice),
            "margin" => desc
                ? query.OrderByDescending(product => product.SalePrice > 0 ? (product.SalePrice - product.CostPrice) / product.SalePrice : 0)
                : query.OrderBy(product => product.SalePrice > 0 ? (product.SalePrice - product.CostPrice) / product.SalePrice : 0),
            "type" => desc ? query.OrderByDescending(product => product.ProductType) : query.OrderBy(product => product.ProductType),
            "status" => desc ? query.OrderByDescending(product => product.IsActive) : query.OrderBy(product => product.IsActive),
            _ => desc ? query.OrderByDescending(product => product.Name) : query.OrderBy(product => product.Name)
        };
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        return sortBy is "code" or "name" or "category" or "brand" or "cost" or "price" or "margin" or "type" or "status"
            ? sortBy
            : "name";
    }

    private async Task ValidateProductAsync(Guid companyId, ProductFormViewModel model, Guid? currentId)
    {
        if (model.UnitOfMeasureId is null)
        {
            ModelState.AddModelError(nameof(model.UnitOfMeasureId), "Selecione uma unidade válida.");
        }

        if (model.ProductType == ProductType.ThirdParty && model.PartnerId is null)
        {
            ModelState.AddModelError(nameof(model.PartnerId), "Produto de terceiro exige parceiro comercial.");
        }

        if (model.CommissionType == CommissionType.Percentage && model.CommissionValue > 100)
        {
            ModelState.AddModelError(nameof(model.CommissionValue), "A comissão percentual não pode ser superior a 100%.");
        }

        var ncm = OnlyDigits(model.Ncm);
        if (ncm is not null && ncm.Length != 8) ModelState.AddModelError(nameof(model.Ncm), "O NCM deve possuir 8 dígitos.");
        var cest = OnlyDigits(model.Cest);
        if (cest is not null && cest.Length != 7) ModelState.AddModelError(nameof(model.Cest), "O CEST deve possuir 7 dígitos.");

        if (await dbContext.Products.AnyAsync(entity => entity.CompanyId == companyId && entity.InternalCode == model.InternalCode.Trim().ToUpper() && entity.Id != currentId))
        {
            ModelState.AddModelError(nameof(model.InternalCode), "Já existe produto com este código interno.");
        }

        var sku = string.IsNullOrWhiteSpace(model.Sku) ? null : model.Sku.Trim().ToUpperInvariant();
        if (sku is not null && await dbContext.Products.AnyAsync(entity => entity.CompanyId == companyId && entity.Sku == sku && entity.Id != currentId))
        {
            ModelState.AddModelError(nameof(model.Sku), "Já existe produto com este SKU.");
        }

        var barcode = OnlyDigits(model.Barcode);
        if (barcode is not null && await dbContext.Products.AnyAsync(entity => entity.CompanyId == companyId && entity.Barcode == barcode && entity.Id != currentId))
        {
            ModelState.AddModelError(nameof(model.Barcode), "Já existe produto com este código de barras.");
        }

        await ValidateRelatedAsync(companyId, model);
    }

    private async Task ValidateRelatedAsync(Guid companyId, ProductFormViewModel model)
    {
        if (model.CategoryId is not null && !await dbContext.Categories.AnyAsync(entity => entity.Id == model.CategoryId && entity.CompanyId == companyId))
            ModelState.AddModelError(nameof(model.CategoryId), "Categoria inválida.");
        if (model.SubcategoryId is not null && !await dbContext.Subcategories.AnyAsync(entity => entity.Id == model.SubcategoryId && entity.CompanyId == companyId && entity.CategoryId == model.CategoryId))
            ModelState.AddModelError(nameof(model.SubcategoryId), "Subcategoria inválida para a categoria selecionada.");
        if (model.BrandId is not null && !await dbContext.Brands.AnyAsync(entity => entity.Id == model.BrandId && entity.CompanyId == companyId))
            ModelState.AddModelError(nameof(model.BrandId), "Marca inválida.");
        if (model.UnitOfMeasureId is not null && !await dbContext.UnitsOfMeasure.AnyAsync(entity => entity.Id == model.UnitOfMeasureId && entity.CompanyId == companyId))
            ModelState.AddModelError(nameof(model.UnitOfMeasureId), "Unidade inválida.");
        if (model.ProductGroupId is not null && !await dbContext.ProductGroups.AnyAsync(entity => entity.Id == model.ProductGroupId && entity.CompanyId == companyId))
            ModelState.AddModelError(nameof(model.ProductGroupId), "Grupo inválido.");
        if (model.MainSupplierId is not null && !await dbContext.Suppliers.AnyAsync(entity => entity.Id == model.MainSupplierId && entity.CompanyId == companyId))
            ModelState.AddModelError(nameof(model.MainSupplierId), "Fornecedor inválido.");
        if (model.PartnerId is not null && !await dbContext.CommercialPartners.AnyAsync(entity => entity.Id == model.PartnerId && entity.CompanyId == companyId))
            ModelState.AddModelError(nameof(model.PartnerId), "Parceiro inválido.");
        if (model.DefaultWarehouseId is not null && !await dbContext.Warehouses.AnyAsync(entity => entity.Id == model.DefaultWarehouseId && entity.CompanyId == companyId))
            ModelState.AddModelError(nameof(model.DefaultWarehouseId), "Depósito inválido.");
        if (model.DefaultWarehouseLocationId is not null && !await dbContext.InternalLocations.AnyAsync(entity => entity.Id == model.DefaultWarehouseLocationId && entity.CompanyId == companyId && entity.WarehouseId == model.DefaultWarehouseId))
            ModelState.AddModelError(nameof(model.DefaultWarehouseLocationId), "Localização inválida para o depósito selecionado.");
    }

    private void ApplyModel(Product product, ProductFormViewModel model)
    {
        var controlsStock = model.ProductType is ProductType.ThirdParty or ProductType.Service ? false : model.ControlsStock;

        product.Update(
            model.InternalCode,
            model.Sku,
            model.Barcode,
            model.Reference,
            model.Name,
            model.ShortDescription,
            model.Description,
            model.ProductType,
            controlsStock,
            model.IsActive,
            model.CategoryId,
            model.SubcategoryId,
            model.BrandId,
            model.UnitOfMeasureId!.Value,
            model.ProductGroupId,
            model.MainSupplierId,
            model.PartnerId,
            controlsStock ? model.DefaultWarehouseId : null,
            controlsStock ? model.DefaultWarehouseLocationId : null,
            model.Ncm,
            model.Cest,
            model.CostPrice,
            model.SalePrice,
            model.CommissionType,
            model.CommissionValue,
            model.PriceValidUntil,
            controlsStock ? model.MinimumStock : null,
            model.Notes);
    }

    private async Task<IActionResult> ChangeStatusAsync(IReadOnlyCollection<Guid> ids, bool activate)
    {
        if (ids.Count == 0)
        {
            TempData["Error"] = "Selecione ao menos um produto.";
            return RedirectToAction(nameof(Index));
        }

        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var products = await dbContext.Products
            .Where(entity => entity.CompanyId == companyId && ids.Contains(entity.Id))
            .ToListAsync();

        foreach (var product in products)
        {
            var oldValue = product.IsActive ? "Ativo" : "Inativo";
            if (activate) product.Activate(); else product.Deactivate();
            var newValue = product.IsActive ? "Ativo" : "Inativo";
            if (oldValue != newValue)
            {
                dbContext.ProductChangeHistories.Add(new ProductChangeHistory(companyId, product.Id, "Status", oldValue, newValue, ids.Count > 1 ? "ação em massa" : "edição"));
            }
        }

        await dbContext.SaveChangesAsync();
        TempData["Success"] = $"{products.Count} produto(s) {(activate ? "ativado(s)" : "inativado(s)")} com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ProductFormViewModel?> BuildFormModelAsync(Guid companyId, Guid id)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(entity => entity.Id == id && entity.CompanyId == companyId)
            .Select(entity => new ProductFormViewModel
            {
                Id = entity.Id,
                InternalCode = entity.InternalCode,
                Sku = entity.Sku,
                Barcode = entity.Barcode,
                Reference = entity.Reference,
                Name = entity.Name,
                ShortDescription = entity.ShortDescription,
                Description = entity.Description,
                ProductType = entity.ProductType,
                ControlsStock = entity.ControlsStock,
                IsActive = entity.IsActive,
                CategoryId = entity.CategoryId,
                SubcategoryId = entity.SubcategoryId,
                BrandId = entity.BrandId,
                UnitOfMeasureId = entity.UnitOfMeasureId,
                ProductGroupId = entity.ProductGroupId,
                MainSupplierId = entity.MainSupplierId,
                PartnerId = entity.PartnerId,
                DefaultWarehouseId = entity.DefaultWarehouseId,
                DefaultWarehouseLocationId = entity.DefaultWarehouseLocationId,
                Ncm = entity.Ncm,
                Cest = entity.Cest,
                CurrentImagePath = entity.ImagePath,
                CostPrice = entity.CostPrice,
                SalePrice = entity.SalePrice,
                CommissionType = entity.CommissionType,
                CommissionValue = entity.CommissionValue,
                PriceValidUntil = entity.PriceValidUntil,
                MinimumStock = entity.MinimumStock,
                Notes = entity.Notes
            })
            .FirstOrDefaultAsync();
    }

    private async Task PopulateOptionsAsync(ProductFormViewModel model, Guid companyId)
    {
        model.Categories = await GetCategoryOptionsAsync(companyId, model.CategoryId);
        model.Subcategories = await GetSubcategoryOptionsAsync(companyId, model.CategoryId, model.SubcategoryId);
        model.Brands = await GetBrandOptionsAsync(companyId, model.BrandId);
        model.Units = await GetUnitOptionsAsync(companyId, model.UnitOfMeasureId);
        model.Groups = await GetGroupOptionsAsync(companyId, model.ProductGroupId);
        model.Suppliers = await GetSupplierOptionsAsync(companyId, model.MainSupplierId);
        model.Partners = await GetPartnerOptionsAsync(companyId, model.PartnerId);
        model.Warehouses = await GetWarehouseOptionsAsync(companyId, model.DefaultWarehouseId);
        model.Locations = await GetLocationOptionsAsync(companyId, model.DefaultWarehouseId, model.DefaultWarehouseLocationId);
    }

    private async Task<IReadOnlyList<SelectListItem>> GetCategoryOptionsAsync(Guid companyId, Guid? selectedId) =>
        await SelectBasic(dbContext.Categories.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.IsActive), selectedId);

    private async Task<IReadOnlyList<SelectListItem>> GetSubcategoryOptionsAsync(Guid companyId, Guid? categoryId, Guid? selectedId) =>
        categoryId is null ? [] : await SelectBasic(dbContext.Subcategories.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.CategoryId == categoryId && entity.IsActive), selectedId);

    private async Task<IReadOnlyList<SelectListItem>> GetBrandOptionsAsync(Guid companyId, Guid? selectedId) =>
        await SelectBasic(dbContext.Brands.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.IsActive), selectedId);

    private async Task<IReadOnlyList<SelectListItem>> GetUnitOptionsAsync(Guid companyId, Guid? selectedId) =>
        await dbContext.UnitsOfMeasure.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.IsActive).OrderBy(entity => entity.Name).Select(entity => new SelectListItem($"{entity.Name} ({entity.Abbreviation})", entity.Id.ToString(), selectedId == entity.Id)).ToListAsync();

    private async Task<IReadOnlyList<SelectListItem>> GetGroupOptionsAsync(Guid companyId, Guid? selectedId) =>
        await SelectBasic(dbContext.ProductGroups.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.IsActive), selectedId);

    private async Task<IReadOnlyList<SelectListItem>> GetSupplierOptionsAsync(Guid companyId, Guid? selectedId) =>
        await dbContext.Suppliers.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.IsActive).OrderBy(entity => entity.LegalName).Select(entity => new SelectListItem(entity.TradeName ?? entity.LegalName, entity.Id.ToString(), selectedId == entity.Id)).ToListAsync();

    private async Task<IReadOnlyList<SelectListItem>> GetPartnerOptionsAsync(Guid companyId, Guid? selectedId) =>
        await dbContext.CommercialPartners.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.IsActive).OrderBy(entity => entity.Name).Select(entity => new SelectListItem(entity.Name, entity.Id.ToString(), selectedId == entity.Id)).ToListAsync();

    private async Task<IReadOnlyList<SelectListItem>> GetWarehouseOptionsAsync(Guid companyId, Guid? selectedId) =>
        await SelectBasic(dbContext.Warehouses.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.IsActive), selectedId);

    private async Task<IReadOnlyList<SelectListItem>> GetLocationOptionsAsync(Guid companyId, Guid? warehouseId, Guid? selectedId) =>
        warehouseId is null ? [] : await SelectBasic(dbContext.InternalLocations.AsNoTracking().Where(entity => entity.CompanyId == companyId && entity.WarehouseId == warehouseId && entity.IsActive), selectedId);

    private static async Task<IReadOnlyList<SelectListItem>> SelectBasic<T>(IQueryable<T> query, Guid? selectedId)
        where T : BasicRegistrationEntity
    {
        return await query.OrderBy(entity => entity.Name)
            .Select(entity => new SelectListItem(entity.Name, entity.Id.ToString(), selectedId == entity.Id))
            .ToListAsync();
    }

    private async Task<string?> SaveImageAsync(IFormFile? imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension) || !AllowedImageContentTypes.Contains(imageFile.ContentType) || imageFile.Length > MaxImageBytes)
        {
            throw new InvalidOperationException("Imagem inválida. Use JPG, PNG ou WEBP com até 2 MB.");
        }

        var directory = Path.Combine(environment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(directory, fileName);

        await using var stream = System.IO.File.Create(physicalPath);
        await imageFile.CopyToAsync(stream);

        return $"/uploads/products/{fileName}";
    }

    private void DeleteImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !imagePath.StartsWith("/uploads/products/", StringComparison.Ordinal))
        {
            return;
        }

        var fileName = Path.GetFileName(imagePath);
        var physicalPath = Path.Combine(environment.WebRootPath, "uploads", "products", fileName);

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    private static IReadOnlyDictionary<string, string?> Snapshot(Product product)
    {
        return new Dictionary<string, string?>
        {
            ["Código"] = product.InternalCode,
            ["Descrição"] = product.Name,
            ["Tipo"] = product.ProductType.ToString(),
            ["Controle de estoque"] = product.ControlsStock.ToString(),
            ["Custo"] = product.CostPrice.ToString("0.####"),
            ["Preço"] = product.SalePrice.ToString("0.####"),
            ["Comissão"] = product.CommissionValue?.ToString("0.####"),
            ["Validade do preço"] = product.PriceValidUntil?.ToString("yyyy-MM-dd"),
            ["Fornecedor"] = product.MainSupplierId?.ToString(),
            ["Parceiro"] = product.PartnerId?.ToString(),
            ["Categoria"] = product.CategoryId?.ToString(),
            ["Localização"] = product.DefaultWarehouseLocationId?.ToString(),
            ["Status"] = product.IsActive ? "Ativo" : "Inativo"
        };
    }

    private void AddHistory(Guid companyId, Product product, IReadOnlyDictionary<string, string?> before, string origin)
    {
        var after = Snapshot(product);

        foreach (var item in before)
        {
            if (item.Value != after[item.Key])
            {
                dbContext.ProductChangeHistories.Add(new ProductChangeHistory(companyId, product.Id, item.Key, item.Value, after[item.Key], origin));
            }
        }
    }

    private static decimal ParseDecimal(string? value)
    {
        return decimal.TryParse(value, out var parsed) ? parsed : throw new FormatException("Valor decimal inválido.");
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : ParseDecimal(value);
    }

    private static DateOnly? ParseNullableDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : DateOnly.Parse(value);
    }

    private static string? OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static string BuildFriendlyError(Exception exception)
    {
        return exception.GetBaseException().Message;
    }
}
