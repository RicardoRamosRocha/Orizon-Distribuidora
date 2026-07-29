using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Application.Commercial;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Commercial;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = Roles.Administrator), Route("Admin/Vendas")]
public sealed class SalesController(ICommercialService service, ICurrentCompanyAccessor companyAccessor) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] SaleFilter filter, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        var normalized = filter with { Page = Math.Max(1, filter.Page), PageSize = Math.Clamp(filter.PageSize, 10, 100) };
        var (page, summary) = await service.ListSalesAsync(companyId, normalized, ct);
        return View(new SaleIndexViewModel { Filter = normalized, Page = page, Summary = summary, Options = await service.GetOptionsAsync(companyId, ct) });
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User); var sale = await service.GetSaleAsync(companyId, id, ct);
        return sale is null ? NotFound() : View(new SaleDetailsViewModel(sale, await service.GetCompanyHeaderAsync(companyId, ct)));
    }
    [HttpPost("{id:guid}/Confirmar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct) => await Change(id, service.ConfirmSaleAsync, "Venda confirmada e estoque baixado por movimentação.", ct);
    [HttpPost("{id:guid}/Cancelar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => await Change(id, service.CancelSaleAsync, "Venda cancelada.", ct);
    [HttpGet("{id:guid}/Comprovante")]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User); var sale = await service.GetSaleAsync(companyId, id, ct);
        return sale is null ? NotFound() : View(new SaleDetailsViewModel(sale, await service.GetCompanyHeaderAsync(companyId, ct)));
    }
    private async Task<IActionResult> Change(Guid id, Func<Guid, Guid?, Guid, CancellationToken, Task<CommercialResult>> op, string success, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        Guid? userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;
        var result = await op(companyId, userId, id, ct); TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? success : result.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }
}
