using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orizon.Distribuidora.Application.Commercial;
using Orizon.Distribuidora.Application.Stock;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Commercial;

public sealed class CommercialServicePostgreSqlTests
{
    [Fact]
    public async Task Main_lists_translate_count_order_paging_and_materialization_on_postgresql()
    {
        await using var db = CreateContext();
        var companyId = await db.Companies.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();
        IStockService stock = new StockService(db);
        ICommercialService service = new CommercialService(db, stock, NullLogger<CommercialService>.Instance);

        var quotes = await service.ListQuotesAsync(companyId,
            new QuoteFilter(Search: "a", SortBy: "total", SortDirection: "desc", Page: 1, PageSize: 10));
        var sales = await service.ListSalesAsync(companyId,
            new SaleFilter(Search: "a", SortBy: "issued", SortDirection: "desc", Page: 1, PageSize: 10));

        Assert.InRange(quotes.Page.Items.Count, 0, 10);
        Assert.True(quotes.Page.TotalCount >= quotes.Page.Items.Count);
        Assert.InRange(sales.Page.Items.Count, 0, 10);
        Assert.True(sales.Page.TotalCount >= sales.Page.Items.Count);
    }

    [Fact]
    public async Task Commercial_flow_is_transactional_idempotent_and_rolls_back_on_postgresql()
    {
        await using var db = CreateContext();
        var product = await db.Products.AsNoTracking()
            .Where(x => x.IsActive && x.ControlsStock && x.ProductType != ProductType.ThirdParty)
            .Select(x => new
            {
                x.CompanyId,
                ProductId = x.Id,
                ProductPrice = x.SalePrice
            })
            .FirstAsync();
        var warehouseId = await db.Warehouses.AsNoTracking()
            .Where(x => x.CompanyId == product.CompanyId && x.IsActive)
            .Select(x => x.Id).FirstAsync();
        var stock = new StockService(db);
        var stockSetup = await stock.RegisterPositiveAdjustmentAsync(product.CompanyId, null,
            new RegisterStockMovementRequest(product.ProductId, warehouseId, 100,
                "Preparação do teste relacional", OperationKey: $"commercial-test:{Guid.NewGuid()}"));
        Assert.True(stockSetup.Succeeded, stockSetup.ErrorMessage);
        db.ChangeTracker.Clear();
        var fixture = new
        {
            product.CompanyId,
            product.ProductId,
            WarehouseId = warehouseId,
            QuantityOnHand = stockSetup.ResultingQuantity,
            product.ProductPrice
        };
        var customerId = await db.CommercialPartners.AsNoTracking()
            .Where(x => x.CompanyId == fixture.CompanyId && x.IsActive)
            .Select(x => x.Id).FirstAsync();
        var service = CreateService(db);
        var validUntil = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));

        var draftRequest = Request(customerId, fixture.ProductId, fixture.WarehouseId, validUntil, 2,
            fixture.ProductPrice, "rascunho relacional");
        var draft = await service.CreateQuoteAsync(fixture.CompanyId, null, draftRequest, false);
        Assert.True(draft.Succeeded, draft.ErrorMessage);
        var savedDraft = await service.GetQuoteAsync(fixture.CompanyId, draft.DocumentId!.Value);
        Assert.NotNull(savedDraft);
        Assert.Equal(QuoteStatus.Draft, savedDraft.Status);
        Assert.Equal(2, savedDraft.Items.Single().Quantity);
        Assert.Equal(savedDraft.Items.Single().Quantity * savedDraft.Items.Single().UnitPrice -
            savedDraft.Items.Single().Discount, savedDraft.Items.Single().Total);
        Assert.Equal(savedDraft.Subtotal - savedDraft.Discount + savedDraft.Freight +
            savedDraft.AdditionalCharges, savedDraft.Total);
        db.ChangeTracker.Clear();
        var savedAndSentDraft = await service.UpdateQuoteAsync(fixture.CompanyId, null, savedDraft.Id,
            draftRequest with { ConcurrencyToken = savedDraft.ConcurrencyToken }, true);
        Assert.True(savedAndSentDraft.Succeeded,
            $"{savedAndSentDraft.ErrorCode}: {savedAndSentDraft.ErrorMessage}");
        db.ChangeTracker.Clear();
        Assert.Equal(QuoteStatus.Sent, (await service.GetQuoteAsync(
            fixture.CompanyId, savedDraft.Id))!.Status);

        var sentRequest = Request(customerId, fixture.ProductId, fixture.WarehouseId, validUntil, 1,
            fixture.ProductPrice, "enviado relacional");
        var sent = await service.CreateQuoteAsync(fixture.CompanyId, null, sentRequest, true);
        Assert.True(sent.Succeeded, sent.ErrorMessage);
        var sentQuote = await service.GetQuoteAsync(fixture.CompanyId, sent.DocumentId!.Value);
        Assert.Equal(QuoteStatus.Sent, sentQuote!.Status);

        var approved = await service.ChangeQuoteStatusAsync(
            fixture.CompanyId, null, sentQuote.Id, QuoteStatus.Approved);
        Assert.True(approved.Succeeded, approved.ErrorMessage);
        var conversion = await service.ConvertQuoteAsync(fixture.CompanyId, null, sentQuote.Id);
        Assert.True(conversion.Succeeded, conversion.ErrorMessage);
        var repeatedConversion = await service.ConvertQuoteAsync(fixture.CompanyId, null, sentQuote.Id);
        Assert.True(repeatedConversion.Succeeded, repeatedConversion.ErrorMessage);
        Assert.Equal(conversion.DocumentId, repeatedConversion.DocumentId);
        Assert.Equal(1, await db.Sales.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId && x.QuoteId == sentQuote.Id));

        var before = await db.StockBalances.AsNoTracking()
            .Where(x => x.CompanyId == fixture.CompanyId && x.ProductId == fixture.ProductId &&
                x.WarehouseId == fixture.WarehouseId)
            .Select(x => x.QuantityOnHand).SingleAsync();
        var confirmed = await service.ConfirmSaleAsync(
            fixture.CompanyId, null, conversion.DocumentId!.Value);
        Assert.True(confirmed.Succeeded, confirmed.ErrorMessage);
        db.ChangeTracker.Clear();
        var after = await db.StockBalances.AsNoTracking()
            .Where(x => x.CompanyId == fixture.CompanyId && x.ProductId == fixture.ProductId &&
                x.WarehouseId == fixture.WarehouseId)
            .Select(x => x.QuantityOnHand).SingleAsync();
        Assert.Equal(before - 1, after);
        Assert.Single(await db.StockMovements.AsNoTracking()
            .Where(x => x.CompanyId == fixture.CompanyId &&
                x.ReferenceId == conversion.DocumentId.Value.ToString()).ToListAsync());

        var repeatedConfirmation = await service.ConfirmSaleAsync(
            fixture.CompanyId, null, conversion.DocumentId.Value);
        Assert.True(repeatedConfirmation.Succeeded, repeatedConfirmation.ErrorMessage);
        db.ChangeTracker.Clear();
        Assert.Equal(after, await db.StockBalances.AsNoTracking()
            .Where(x => x.CompanyId == fixture.CompanyId && x.ProductId == fixture.ProductId &&
                x.WarehouseId == fixture.WarehouseId)
            .Select(x => x.QuantityOnHand).SingleAsync());
        Assert.Single(await db.StockMovements.AsNoTracking()
            .Where(x => x.CompanyId == fixture.CompanyId &&
                x.ReferenceId == conversion.DocumentId.Value.ToString()).ToListAsync());

        var failingSent = await service.CreateQuoteAsync(fixture.CompanyId, null,
            Request(customerId, fixture.ProductId, fixture.WarehouseId, validUntil, after + 1,
                fixture.ProductPrice, "rollback relacional"), true);
        Assert.True(failingSent.Succeeded, failingSent.ErrorMessage);
        Assert.True((await service.ChangeQuoteStatusAsync(
            fixture.CompanyId, null, failingSent.DocumentId!.Value, QuoteStatus.Approved)).Succeeded);
        var failingConversion = await service.ConvertQuoteAsync(
            fixture.CompanyId, null, failingSent.DocumentId.Value);
        Assert.True(failingConversion.Succeeded, failingConversion.ErrorMessage);
        var movementCountBeforeFailure = await db.StockMovements.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId);

        var failedConfirmation = await service.ConfirmSaleAsync(
            fixture.CompanyId, null, failingConversion.DocumentId!.Value);
        Assert.False(failedConfirmation.Succeeded);
        db.ChangeTracker.Clear();
        Assert.Equal(SaleStatus.Draft, await db.Sales.AsNoTracking()
            .Where(x => x.Id == failingConversion.DocumentId.Value).Select(x => x.Status).SingleAsync());
        Assert.Equal(movementCountBeforeFailure,
            await db.StockMovements.AsNoTracking().CountAsync(x => x.CompanyId == fixture.CompanyId));
        Assert.Equal(after, await db.StockBalances.AsNoTracking()
            .Where(x => x.CompanyId == fixture.CompanyId && x.ProductId == fixture.ProductId &&
                x.WarehouseId == fixture.WarehouseId)
            .Select(x => x.QuantityOnHand).SingleAsync());
    }

    [Fact]
    public async Task Quote_creation_persists_once_and_leaves_no_partial_records_on_failure()
    {
        await using var db = CreateContext();
        var fixture = await db.Products.AsNoTracking()
            .Where(x => x.IsActive && x.ControlsStock)
            .Select(x => new
            {
                x.CompanyId,
                ProductId = x.Id,
                CustomerId = db.CommercialPartners
                    .Where(customer => customer.CompanyId == x.CompanyId && customer.IsActive)
                    .Select(customer => customer.Id)
                    .First()
            })
            .FirstAsync();
        var warehouseId = await db.Warehouses.AsNoTracking()
            .Where(x => x.CompanyId == fixture.CompanyId && x.IsActive)
            .Select(x => x.Id)
            .FirstAsync();
        var service = CreateService(db);
        var marker = $"criação-atômica-{Guid.NewGuid()}";
        var request = Request(fixture.CustomerId, fixture.ProductId, warehouseId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), 2, 10, marker);

        var created = await service.CreateQuoteAsync(fixture.CompanyId, null, request, false);

        Assert.True(created.Succeeded, created.ErrorMessage);
        Assert.Equal(1, await db.Quotes.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId && x.Notes == marker));
        Assert.Equal(1, await db.QuoteItems.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId &&
                x.QuoteId == created.DocumentId!.Value));

        var quoteCountBeforeFailure = await db.Quotes.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId);
        var itemCountBeforeFailure = await db.QuoteItems.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId);
        var failed = await service.CreateQuoteAsync(fixture.CompanyId, null,
            request with
            {
                Notes = $"{marker}-falha",
                Items =
                [
                    new CommercialItemInput(fixture.ProductId, 1, 10, 0, Guid.NewGuid())
                ]
            }, true);

        Assert.False(failed.Succeeded);
        Assert.Equal("persistence_error", failed.ErrorCode);
        Assert.Equal(
            "Não foi possível salvar o orçamento. Tente novamente; se o problema continuar, contate o suporte.",
            failed.ErrorMessage);
        Assert.Equal(quoteCountBeforeFailure, await db.Quotes.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId));
        Assert.Equal(itemCountBeforeFailure, await db.QuoteItems.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId));
        Assert.Equal(0, await db.Quotes.AsNoTracking()
            .CountAsync(x => x.CompanyId == fixture.CompanyId && x.Notes == $"{marker}-falha"));
    }

    private static ApplicationDbContext CreateContext()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection, options => options.EnableRetryOnFailure()).Options);
    }

    private static ICommercialService CreateService(ApplicationDbContext db) =>
        new CommercialService(db, new StockService(db),
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<CommercialService>());

    private static SaveQuoteRequest Request(
        Guid customerId,
        Guid productId,
        Guid warehouseId,
        DateOnly validUntil,
        decimal quantity,
        decimal unitPrice,
        string notes) =>
        new(customerId, validUntil, null, 0, 0, 0, notes, null,
            [new CommercialItemInput(productId, quantity, unitPrice, 0, warehouseId)], null);
}
