using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Orizon.Distribuidora.Application.Stock;
using Orizon.Distribuidora.Web.Areas.Admin.Controllers;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Stock;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Application.Tests.Stock;

public sealed class StockControllerTests
{
    [Fact]
    public void WriteActions_RequireAntiforgery()
    {
        foreach (var name in new[] { nameof(StockController.RegisterEntry), nameof(StockController.RegisterIssue),
                     nameof(StockController.RegisterPositiveAdjustment), nameof(StockController.RegisterNegativeAdjustment) })
            Assert.NotNull(typeof(StockController).GetMethod(name)!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
    }

    [Fact]
    public async Task InvalidModel_DoesNotMoveStock()
    {
        var service = new FakeStockService();
        var controller = Create(service);
        controller.ModelState.AddModelError("Quantity", "Inválida");
        var result = await controller.RegisterIssue(new StockMovementFormViewModel(), default);
        Assert.IsType<RedirectResult>(result);
        Assert.Equal(0, service.OperationCalls);
    }

    [Theory]
    [InlineData("entry")]
    [InlineData("issue")]
    [InlineData("positive")]
    [InlineData("negative")]
    public async Task ValidOperation_CallsDistinctService(string operation)
    {
        var service = new FakeStockService();
        var controller = Create(service);
        var model = new StockMovementFormViewModel { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), Quantity = 2, Reason = "Teste" };
        _ = operation switch
        {
            "entry" => await controller.RegisterEntry(model, default),
            "issue" => await controller.RegisterIssue(model, default),
            "positive" => await controller.RegisterPositiveAdjustment(model, default),
            _ => await controller.RegisterNegativeAdjustment(model, default)
        };
        Assert.Equal(operation, service.LastOperation);
    }

    [Fact]
    public async Task BusinessError_IsPresentedWithoutSuccess()
    {
        var service = new FakeStockService { Result = StockOperationResult.Failure("insufficient_stock", "technical") };
        var controller = Create(service);
        await controller.RegisterIssue(new() { ProductId = Guid.NewGuid(), WarehouseId = Guid.NewGuid(), Quantity = 9, Reason = "Venda" }, default);
        Assert.Contains("Saldo insuficiente", controller.TempData["Error"]?.ToString());
        Assert.Null(controller.TempData["Success"]);
    }

    private static StockController Create(FakeStockService service)
    {
        var controller = new StockController(service, new FakeCompanyAccessor())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), new MemoryTempDataProvider())
        };
        controller.Url = new FakeUrlHelper();
        return controller;
    }

    private sealed class FakeCompanyAccessor : ICurrentCompanyAccessor
    {
        public Task<Guid> GetCurrentCompanyIdAsync(System.Security.Claims.ClaimsPrincipal user) => Task.FromResult(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    }

    private sealed class FakeStockService : IStockService
    {
        public int OperationCalls { get; private set; }
        public string? LastOperation { get; private set; }
        public StockOperationResult Result { get; set; } = StockOperationResult.Success(Guid.NewGuid(), 2);
        private Task<StockOperationResult> Op(string name) { OperationCalls++; LastOperation = name; return Task.FromResult(Result); }
        public Task<StockOperationResult> RegisterStockEntryAsync(Guid c, Guid? u, RegisterStockMovementRequest r, CancellationToken t = default) => Op("entry");
        public Task<StockOperationResult> RegisterStockIssueAsync(Guid c, Guid? u, RegisterStockMovementRequest r, CancellationToken t = default) => Op("issue");
        public Task<StockOperationResult> RegisterPositiveAdjustmentAsync(Guid c, Guid? u, RegisterStockMovementRequest r, CancellationToken t = default) => Op("positive");
        public Task<StockOperationResult> RegisterNegativeAdjustmentAsync(Guid c, Guid? u, RegisterStockMovementRequest r, CancellationToken t = default) => Op("negative");
        public Task<StockOperationResult> RegisterInitialBalanceAsync(Guid c, Guid? u, RegisterStockMovementRequest r, CancellationToken t = default) => Op("initial");
        public Task<StockOperationResult> RegisterStockIssueBatchAsync(Guid c, Guid? u, RegisterStockIssueBatchRequest r, CancellationToken t = default) => Op("batch");
        public Task<StockBalanceDto?> GetStockBalanceAsync(Guid c, Guid p, Guid w, CancellationToken t = default) => Task.FromResult<StockBalanceDto?>(null);
        public Task<PagedResult<StockBalanceDto>> ListStockBalancesAsync(Guid c, StockBalanceFilter f, CancellationToken t = default) => Task.FromResult(new PagedResult<StockBalanceDto>([], 1, 25, 0));
        public Task<PagedResult<StockMovementDto>> ListStockMovementsAsync(Guid c, StockMovementFilter f, CancellationToken t = default) => Task.FromResult(new PagedResult<StockMovementDto>([], 1, 25, 0));
        public Task<StockDashboardSummary> GetDashboardSummaryAsync(Guid c, CancellationToken t = default) => Task.FromResult(new StockDashboardSummary(0, 0, 0, 0, 0, 0));
        public Task<StockWorkspaceOptions> GetWorkspaceOptionsAsync(Guid c, CancellationToken t = default) => Task.FromResult(new StockWorkspaceOptions([], [], [], []));
    }

    private sealed class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new();
        public string? Action(UrlActionContext context) => "/Admin/Estoque";
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => url?.StartsWith('/') == true;
        public string? Link(string? routeName, object? values) => null;
        public string? RouteUrl(UrlRouteContext context) => null;
    }

    private sealed class MemoryTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        private Dictionary<string, object> values = [];
        public IDictionary<string, object> LoadTempData(HttpContext context) => values;
        public void SaveTempData(HttpContext context, IDictionary<string, object> data) => values = new(data);
    }
}
