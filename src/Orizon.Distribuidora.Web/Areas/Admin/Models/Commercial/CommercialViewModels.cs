using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Application.Commercial;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Commercial;

public sealed class QuoteIndexViewModel
{
    public required QuoteFilter Filter { get; init; }
    public required CommercialPage<QuoteListItem> Page { get; init; }
    public required QuoteSummary Summary { get; init; }
    public required CommercialOptions Options { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Page.TotalCount / (decimal)Page.PageSize));
}
public sealed class SaleIndexViewModel
{
    public required SaleFilter Filter { get; init; }
    public required CommercialPage<SaleListItem> Page { get; init; }
    public required SaleSummary Summary { get; init; }
    public required CommercialOptions Options { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Page.TotalCount / (decimal)Page.PageSize));
}
public sealed class QuoteFormViewModel
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage = "Selecione o cliente.")] public Guid? CustomerId { get; set; }
    [Required(ErrorMessage = "Informe a validade.")] public DateOnly ValidUntil { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
    public Guid? PriceTableId { get; set; }
    [ModelBinder(BinderType = typeof(CommercialDecimalModelBinder)), Range(typeof(decimal), "0", "999999999")] public decimal Discount { get; set; }
    [ModelBinder(BinderType = typeof(CommercialDecimalModelBinder)), Range(typeof(decimal), "0", "999999999")] public decimal Freight { get; set; }
    [ModelBinder(BinderType = typeof(CommercialDecimalModelBinder)), Range(typeof(decimal), "0", "999999999")] public decimal AdditionalCharges { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
    [StringLength(1000)] public string? DeliveryAddress { get; set; }
    public Guid? ConcurrencyToken { get; set; }
    public List<QuoteItemFormViewModel> Items { get; set; } = [];
    public CommercialOptions Options { get; set; } = new([], [], []);
}
public sealed class QuoteItemFormViewModel
{
    [Required] public Guid? ProductId { get; set; }
    public string? ProductLabel { get; set; }
    [ModelBinder(BinderType = typeof(CommercialDecimalModelBinder)), Range(typeof(decimal), "1", "999999999", ErrorMessage = "A quantidade deve ser igual ou maior que 1.")] public decimal Quantity { get; set; } = 1;
    [ModelBinder(BinderType = typeof(CommercialDecimalModelBinder)), Range(typeof(decimal), "0", "999999999")] public decimal? UnitPrice { get; set; }
    [ModelBinder(BinderType = typeof(CommercialDecimalModelBinder)), Range(typeof(decimal), "0", "999999999")] public decimal Discount { get; set; }
    public Guid? WarehouseId { get; set; }
}
public sealed record QuoteDetailsViewModel(QuoteDetail Quote, CompanyDocumentHeader? Company);
public sealed record SaleDetailsViewModel(SaleDetail Sale, CompanyDocumentHeader? Company);
