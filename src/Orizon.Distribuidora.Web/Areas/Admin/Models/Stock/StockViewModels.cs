using System.ComponentModel.DataAnnotations;
using Orizon.Distribuidora.Application.Stock;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Stock;

public sealed class StockIndexFilterViewModel
{
    public string? Search { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? CategoryId { get; set; }
    public StockLevelStatus Status { get; set; }
    public bool OnlyActive { get; set; } = true;
    public string SortBy { get; set; } = "product";
    public string SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class StockIndexViewModel
{
    public required StockIndexFilterViewModel Filter { get; init; }
    public required PagedResult<StockBalanceDto> Balances { get; init; }
    public required StockDashboardSummary Summary { get; init; }
    public required StockWorkspaceOptions Options { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Balances.TotalCount / (decimal)Balances.PageSize));
}

public sealed class StockMovementFormViewModel
{
    [Required(ErrorMessage = "Selecione o produto.")] public Guid? ProductId { get; set; }
    [Required(ErrorMessage = "Selecione o depósito.")] public Guid? WarehouseId { get; set; }
    public Guid? InternalLocationId { get; set; }
    [Range(typeof(decimal), "0.0001", "999999999", ErrorMessage = "Informe uma quantidade maior que zero.")]
    public decimal Quantity { get; set; }
    [Required(ErrorMessage = "Informe o motivo."), StringLength(200)] public string Reason { get; set; } = string.Empty;
    [StringLength(100)] public string? DocumentNumber { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "O custo não pode ser negativo.")] public decimal? UnitCost { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class StockHistoryFilterViewModel
{
    public string? Search { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public StockMovementType? Type { get; set; }
    public StockMovementDirection? Direction { get; set; }
    [DataType(DataType.Date)] public DateTimeOffset? From { get; set; }
    [DataType(DataType.Date)] public DateTimeOffset? To { get; set; }
    public string? DocumentOrReference { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class StockHistoryViewModel
{
    public required StockHistoryFilterViewModel Filter { get; init; }
    public required PagedResult<StockMovementDto> Movements { get; init; }
    public required StockWorkspaceOptions Options { get; init; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Movements.TotalCount / (decimal)Movements.PageSize));
}
