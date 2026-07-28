using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Application.Stock;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Stock;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
[Route("Admin/Estoque")]
public sealed class StockController(IStockService stockService, ICurrentCompanyAccessor companyAccessor) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] StockIndexFilterViewModel filter, CancellationToken cancellationToken)
    {
        Normalize(filter);
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        var balances = await stockService.ListStockBalancesAsync(companyId,
            new(null, filter.WarehouseId, filter.CategoryId, false, filter.OnlyActive, filter.Page, filter.PageSize,
                filter.Search, filter.Status, filter.SortBy, filter.SortDirection), cancellationToken);
        var summary = await stockService.GetDashboardSummaryAsync(companyId, cancellationToken);
        var options = await stockService.GetWorkspaceOptionsAsync(companyId, cancellationToken);
        return View(new StockIndexViewModel
        {
            Filter = filter, Balances = balances, Summary = summary, Options = options
        });
    }

    [HttpGet("Movimentacoes")]
    public async Task<IActionResult> Movements([FromQuery] StockHistoryFilterViewModel filter, CancellationToken cancellationToken)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 10, 100);
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        var movements = await stockService.ListStockMovementsAsync(companyId,
            new(filter.ProductId, filter.WarehouseId, filter.Type, filter.Direction, filter.From,
                filter.To?.AddDays(1).AddTicks(-1), filter.DocumentOrReference, filter.Page, filter.PageSize, filter.Search), cancellationToken);
        var options = await stockService.GetWorkspaceOptionsAsync(companyId, cancellationToken);
        return View(new StockHistoryViewModel { Filter = filter, Movements = movements, Options = options });
    }

    [HttpPost("Entrada"), ValidateAntiForgeryToken]
    public Task<IActionResult> RegisterEntry(StockMovementFormViewModel model, CancellationToken cancellationToken) =>
        Register(model, stockService.RegisterStockEntryAsync, "Entrada registrada com sucesso.", cancellationToken);

    [HttpPost("Saida"), ValidateAntiForgeryToken]
    public Task<IActionResult> RegisterIssue(StockMovementFormViewModel model, CancellationToken cancellationToken) =>
        Register(model, stockService.RegisterStockIssueAsync, "Saída registrada com sucesso.", cancellationToken);

    [HttpPost("Ajuste/Positivo"), ValidateAntiForgeryToken]
    public Task<IActionResult> RegisterPositiveAdjustment(StockMovementFormViewModel model, CancellationToken cancellationToken) =>
        Register(model, stockService.RegisterPositiveAdjustmentAsync, "Ajuste positivo registrado com sucesso.", cancellationToken);

    [HttpPost("Ajuste/Negativo"), ValidateAntiForgeryToken]
    public Task<IActionResult> RegisterNegativeAdjustment(StockMovementFormViewModel model, CancellationToken cancellationToken) =>
        Register(model, stockService.RegisterNegativeAdjustmentAsync, "Ajuste negativo registrado com sucesso.", cancellationToken);

    private async Task<IActionResult> Register(StockMovementFormViewModel model,
        Func<Guid, Guid?, RegisterStockMovementRequest, CancellationToken, Task<StockOperationResult>> operation,
        string successMessage, CancellationToken cancellationToken)
    {
        var returnUrl = SafeReturnUrl(model.ReturnUrl);
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return Redirect(returnUrl);
        }
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;
        var request = new RegisterStockMovementRequest(model.ProductId!.Value, model.WarehouseId!.Value,
            model.Quantity, model.Reason, model.InternalLocationId, model.Notes, model.UnitCost,
            DocumentNumber: model.DocumentNumber, OperationKey: Guid.NewGuid().ToString("N"));
        var result = await operation(companyId, userId, request, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = FriendlyError(result);
            return Redirect(returnUrl);
        }
        TempData["Success"] = successMessage;
        return Redirect(returnUrl);
    }

    private string SafeReturnUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Url.IsLocalUrl(value) ? value : Url.Action(nameof(Index))!;

    private static string FriendlyError(StockOperationResult result) => result.ErrorCode switch
    {
        "insufficient_stock" => "Saldo insuficiente. Atualize a página e confira o saldo disponível.",
        "concurrency_conflict" => "O saldo mudou durante a operação. Atualize a página e tente novamente.",
        "stock_not_controlled" => "Este produto não possui controle de estoque.",
        "warehouse_not_found" => "O depósito informado não é válido.",
        "location_not_found" => "A localização não pertence ao depósito informado.",
        _ => result.ErrorMessage ?? "Não foi possível registrar a movimentação."
    };

    private static void Normalize(StockIndexFilterViewModel filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 10, 100);
        filter.SortBy = new[] { "product", "code", "warehouse", "quantity" }.Contains(filter.SortBy) ? filter.SortBy : "product";
        filter.SortDirection = filter.SortDirection == "desc" ? "desc" : "asc";
    }
}
