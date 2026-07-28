using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Application.Commercial;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Commercial;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = Roles.Administrator), Route("Admin/Orcamentos")]
public sealed class QuotesController(ICommercialService service, ICurrentCompanyAccessor companyAccessor) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] QuoteFilter filter, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        var normalized = filter with { Page = Math.Max(1, filter.Page), PageSize = Math.Clamp(filter.PageSize, 10, 100) };
        var (page, summary) = await service.ListQuotesAsync(companyId, normalized, ct);
        return View(new QuoteIndexViewModel { Filter = normalized, Page = page, Summary = summary, Options = await service.GetOptionsAsync(companyId, ct) });
    }

    [HttpGet("Novo")]
    public async Task<IActionResult> New(CancellationToken ct)
    {
        var model = new QuoteFormViewModel();
        model.Options = await service.GetOptionsAsync(await companyAccessor.GetCurrentCompanyIdAsync(User), ct);
        return View("Form", model);
    }

    [HttpPost(""), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuoteFormViewModel model, string? intent, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        if (!ModelState.IsValid) return await InvalidForm(model, companyId, ct);
        var result = await service.CreateQuoteAsync(companyId, UserId(), ToRequest(model), intent == "send", ct);
        if (!result.Succeeded) { ModelState.AddModelError("", result.ErrorMessage!); return await InvalidForm(model, companyId, ct); }
        TempData["Success"] = intent == "send" ? "Orçamento salvo e marcado como enviado." : "Rascunho salvo.";
        return RedirectToAction(nameof(Details), new { id = result.DocumentId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        var quote = await service.GetQuoteAsync(companyId, id, ct);
        return quote is null ? NotFound() : View(new QuoteDetailsViewModel(quote, await service.GetCompanyHeaderAsync(companyId, ct)));
    }

    [HttpGet("{id:guid}/Editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        var q = await service.GetQuoteAsync(companyId, id, ct);
        if (q is null) return NotFound();
        if (q.Status != QuoteStatus.Draft) { TempData["Error"] = "Somente rascunhos podem ser editados."; return RedirectToAction(nameof(Details), new { id }); }
        return View("Form", new QuoteFormViewModel { Id = q.Id, CustomerId = q.CustomerId, ValidUntil = q.ValidUntil, PriceTableId = q.PriceTableId,
            Discount = q.Discount, Freight = q.Freight, AdditionalCharges = q.AdditionalCharges, Notes = q.Notes, DeliveryAddress = q.DeliveryAddress,
            ConcurrencyToken = q.ConcurrencyToken, Options = await service.GetOptionsAsync(companyId, ct),
            Items = q.Items.Select(x => new QuoteItemFormViewModel { ProductId = x.ProductId, ProductLabel = $"{x.ProductCode} · {x.Description}", Quantity = x.Quantity, UnitPrice = x.UnitPrice, Discount = x.Discount, WarehouseId = x.WarehouseId }).ToList() });
    }

    [HttpPost("{id:guid}/Editar"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, QuoteFormViewModel model, string? intent, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User); model.Id = id;
        if (!ModelState.IsValid) return await InvalidForm(model, companyId, ct);
        var result = await service.UpdateQuoteAsync(companyId, UserId(), id, ToRequest(model), intent == "send", ct);
        if (!result.Succeeded) { ModelState.AddModelError("", result.ErrorMessage!); return await InvalidForm(model, companyId, ct); }
        TempData["Success"] = "Orçamento atualizado."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/Enviar"), ValidateAntiForgeryToken] public Task<IActionResult> Send(Guid id, CancellationToken ct) => Change(id, QuoteStatus.Sent, "Orçamento marcado como enviado.", ct);
    [HttpPost("{id:guid}/Aprovar"), ValidateAntiForgeryToken] public Task<IActionResult> Approve(Guid id, CancellationToken ct) => Change(id, QuoteStatus.Approved, "Orçamento aprovado.", ct);
    [HttpPost("{id:guid}/Recusar"), ValidateAntiForgeryToken] public Task<IActionResult> Reject(Guid id, CancellationToken ct) => Change(id, QuoteStatus.Rejected, "Orçamento recusado.", ct);
    [HttpPost("{id:guid}/Cancelar"), ValidateAntiForgeryToken] public Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Change(id, QuoteStatus.Cancelled, "Orçamento cancelado.", ct);

    [HttpPost("{id:guid}/Converter"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(Guid id, CancellationToken ct)
    {
        var result = await service.ConvertQuoteAsync(await companyAccessor.GetCurrentCompanyIdAsync(User), UserId(), id, ct);
        if (!result.Succeeded) { TempData["Error"] = result.ErrorMessage; return RedirectToAction(nameof(Details), new { id }); }
        TempData["Success"] = "Orçamento convertido em venda sem redigitação.";
        return RedirectToAction("Details", "Sales", new { id = result.DocumentId });
    }

    [HttpGet("{id:guid}/Imprimir")]
    public async Task<IActionResult> Print(Guid id, CancellationToken ct)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User); var quote = await service.GetQuoteAsync(companyId, id, ct);
        return quote is null ? NotFound() : View(new QuoteDetailsViewModel(quote, await service.GetCompanyHeaderAsync(companyId, ct)));
    }

    [HttpGet("Produtos")]
    public async Task<IActionResult> Products(string? q, Guid? priceTableId, CancellationToken ct) =>
        Json(await service.SearchProductsAsync(await companyAccessor.GetCurrentCompanyIdAsync(User), q, priceTableId, ct));

    private async Task<IActionResult> Change(Guid id, QuoteStatus status, string success, CancellationToken ct)
    {
        var result = await service.ChangeQuoteStatusAsync(await companyAccessor.GetCurrentCompanyIdAsync(User), UserId(), id, status, ct);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? success : result.ErrorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }
    private async Task<IActionResult> InvalidForm(QuoteFormViewModel model, Guid companyId, CancellationToken ct) { model.Options = await service.GetOptionsAsync(companyId, ct); return View("Form", model); }
    private static SaveQuoteRequest ToRequest(QuoteFormViewModel m) => new(m.CustomerId!.Value, m.ValidUntil, m.PriceTableId, m.Discount, m.Freight, m.AdditionalCharges, m.Notes, m.DeliveryAddress,
        m.Items.Where(x => x.ProductId.HasValue).Select(x => new CommercialItemInput(x.ProductId!.Value, x.Quantity, x.UnitPrice, x.Discount, x.WarehouseId)).ToList(), m.ConcurrencyToken);
    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
