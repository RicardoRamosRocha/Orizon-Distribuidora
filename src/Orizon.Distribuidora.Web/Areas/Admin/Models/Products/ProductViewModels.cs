using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Products;

public enum ProductPriceStatus
{
    All = 0,
    Valid = 1,
    Expired = 2,
    NoValidity = 3
}

public sealed class ProductFilterViewModel
{
    public string? Search { get; set; }

    public ProductType? ProductType { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? BrandId { get; set; }

    public Guid? GroupId { get; set; }

    public Guid? SupplierId { get; set; }

    public Guid? PartnerId { get; set; }

    public bool? ControlsStock { get; set; }

    public decimal? MinimumPrice { get; set; }

    public decimal? MaximumPrice { get; set; }

    public ProductPriceStatus PriceStatus { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? SortBy { get; set; } = "name";

    public string? SortDirection { get; set; } = "asc";
}

public sealed class ProductIndexViewModel
{
    public ProductFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<ProductRowViewModel> Items { get; set; } = [];

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public int PageStart { get; set; }

    public int PageEnd { get; set; }

    public bool HasPreviousPage => Filter.Page > 1;

    public bool HasNextPage => Filter.Page < TotalPages;

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];

    public IReadOnlyList<SelectListItem> Brands { get; set; } = [];

    public IReadOnlyList<SelectListItem> Groups { get; set; } = [];

    public IReadOnlyList<SelectListItem> Suppliers { get; set; } = [];

    public IReadOnlyList<SelectListItem> Partners { get; set; } = [];
}

public sealed class ProductRowViewModel
{
    public Guid Id { get; set; }

    public string InternalCode { get; set; } = string.Empty;

    public string? Sku { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public ProductType ProductType { get; set; }

    public bool ControlsStock { get; set; }

    public string? CategoryName { get; set; }

    public string? BrandName { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }

    public decimal SalePrice { get; set; }

    public decimal MarginPercentage { get; set; }

    public DateOnly? PriceValidUntil { get; set; }

    public decimal? MinimumStock { get; set; }

    public bool IsActive { get; set; }

    public string? ImagePath { get; set; }
}

public sealed class ProductFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "O código interno é obrigatório.")]
    [StringLength(50)]
    public string InternalCode { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Sku { get; set; }

    [StringLength(32)]
    public string? Barcode { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    [Required(ErrorMessage = "A descrição principal é obrigatória.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ShortDescription { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public ProductType ProductType { get; set; } = ProductType.Own;

    public bool ControlsStock { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public Guid? CategoryId { get; set; }

    public Guid? SubcategoryId { get; set; }

    public Guid? BrandId { get; set; }

    [Required(ErrorMessage = "A unidade de medida é obrigatória.")]
    public Guid? UnitOfMeasureId { get; set; }

    public Guid? ProductGroupId { get; set; }

    public Guid? MainSupplierId { get; set; }

    public Guid? PartnerId { get; set; }

    public Guid? DefaultWarehouseId { get; set; }

    public Guid? DefaultWarehouseLocationId { get; set; }

    [StringLength(20)]
    public string? Ncm { get; set; }

    [StringLength(20)]
    public string? Cest { get; set; }

    public string? CurrentImagePath { get; set; }

    public bool RemoveImage { get; set; }

    [Range(0, 999999999, ErrorMessage = "O preço de custo não pode ser negativo.")]
    public decimal CostPrice { get; set; }

    [Range(0, 999999999, ErrorMessage = "O preço de venda não pode ser negativo.")]
    public decimal SalePrice { get; set; }

    public CommissionType? CommissionType { get; set; }

    [Range(0, 999999999, ErrorMessage = "A comissão não pode ser negativa.")]
    public decimal? CommissionValue { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? PriceValidUntil { get; set; }

    [Range(0, 999999999, ErrorMessage = "O estoque mínimo não pode ser negativo.")]
    public decimal? MinimumStock { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];

    public IReadOnlyList<SelectListItem> Subcategories { get; set; } = [];

    public IReadOnlyList<SelectListItem> Brands { get; set; } = [];

    public IReadOnlyList<SelectListItem> Units { get; set; } = [];

    public IReadOnlyList<SelectListItem> Groups { get; set; } = [];

    public IReadOnlyList<SelectListItem> Suppliers { get; set; } = [];

    public IReadOnlyList<SelectListItem> Partners { get; set; } = [];

    public IReadOnlyList<SelectListItem> Warehouses { get; set; } = [];

    public IReadOnlyList<SelectListItem> Locations { get; set; } = [];
}

public sealed class ProductHistoryViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public IReadOnlyList<ProductHistoryRowViewModel> Items { get; set; } = [];
}

public sealed class ProductHistoryRowViewModel
{
    public DateTimeOffset CreatedAt { get; set; }

    public string FieldName { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string Origin { get; set; } = string.Empty;
}

public sealed class ProductInlineEditRequest
{
    public Guid Id { get; set; }

    public string Field { get; set; } = string.Empty;

    public string? Value { get; set; }
}

public sealed class ProductGridPreferenceRequest
{
    [Required]
    public string StateJson { get; set; } = "{}";
}

public sealed class ProductSavedFilterRequest
{
    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string FilterJson { get; set; } = "{}";
}
