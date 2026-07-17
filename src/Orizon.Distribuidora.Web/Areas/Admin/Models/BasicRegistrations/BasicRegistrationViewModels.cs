using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.BasicRegistrations;

public sealed class BasicRegistrationIndexViewModel
{
    public required BasicRegistrationModule Module { get; init; }

    public string? Search { get; init; }

    public bool? IsActive { get; init; }

    public int? Type { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? WarehouseId { get; init; }

    public bool? IsSystem { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalItems { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));

    public IReadOnlyList<BasicRegistrationRowViewModel> Items { get; init; } =
        [];

    public IReadOnlyList<SelectListItem> Categories { get; init; } = [];

    public IReadOnlyList<SelectListItem> Warehouses { get; init; } = [];
}

public sealed class BasicRegistrationRowViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Code { get; init; }

    public string? Secondary { get; init; }

    public string? Document { get; init; }

    public string? RelatedName { get; init; }

    public bool IsActive { get; init; }

    public bool IsSystem { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class BasicRegistrationFormViewModel
{
    public Guid? Id { get; set; }

    public string ModuleKey { get; set; } = string.Empty;

    public BasicRegistrationModule? Module { get; set; }

    [StringLength(200, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Code { get; set; }

    [StringLength(500, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? WarehouseId { get; set; }

    [StringLength(20, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Abbreviation { get; set; }

    [Range(0, 6, ErrorMessage = "As casas decimais devem estar entre 0 e 6.")]
    public int DecimalPlaces { get; set; }

    public bool AllowsFraction { get; set; }

    public PersonType Type { get; set; } = PersonType.Company;

    [StringLength(200, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? LegalName { get; set; }

    [StringLength(150, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? TradeName { get; set; }

    [StringLength(18, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Document { get; set; }

    [StringLength(30, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? StateRegistration { get; set; }

    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(150, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Phone { get; set; }

    [StringLength(30, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? MobilePhone { get; set; }

    [StringLength(150, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? ContactName { get; set; }

    [StringLength(20, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? ZipCode { get; set; }

    [StringLength(200, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Street { get; set; }

    [StringLength(20, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Number { get; set; }

    [StringLength(100, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Complement { get; set; }

    [StringLength(100, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Neighborhood { get; set; }

    [StringLength(100, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? City { get; set; }

    [StringLength(2, MinimumLength = 2, ErrorMessage = "A UF deve possuir dois caracteres.")]
    public string? State { get; set; }

    [StringLength(1000, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Notes { get; set; }

    [Range(0, 100, ErrorMessage = "O percentual deve estar entre 0 e 100.")]
    public decimal CommissionPercentage { get; set; }

    public bool IsDefault { get; set; }

    [StringLength(50, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Aisle { get; set; }

    [StringLength(50, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Section { get; set; }

    [StringLength(50, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Shelf { get; set; }

    [StringLength(50, ErrorMessage = "Informe no máximo {1} caracteres.")]
    public string? Position { get; set; }

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Informe uma cor hexadecimal no formato #RRGGBB.")]
    public string? Color { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "A ordem deve ser igual ou maior que zero.")]
    public int SortOrder { get; set; }

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];

    public IReadOnlyList<SelectListItem> Warehouses { get; set; } = [];
}
