using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orizon.Distribuidora.Application.Commercial;
using Orizon.Distribuidora.Application.Stock;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class CommercialService(
    ApplicationDbContext db,
    IStockService stockService,
    ILogger<CommercialService> logger) : ICommercialService
{
    public async Task<(CommercialPage<QuoteListItem> Page, QuoteSummary Summary)> ListQuotesAsync(
        Guid companyId, QuoteFilter filter, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = db.Quotes.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.CustomerName, $"%{term}%") ||
                EF.Functions.ILike(x.Number.ToString(), $"%{term}%"));
        }
        if (filter.CustomerId.HasValue) query = query.Where(x => x.CustomerId == filter.CustomerId);
        if (filter.From.HasValue) query = query.Where(x => x.IssuedAt >= filter.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (filter.To.HasValue) query = query.Where(x => x.IssuedAt < filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (filter.ValidUntil.HasValue) query = query.Where(x => x.ValidUntil == filter.ValidUntil);
        if (filter.SellerUserId.HasValue) query = query.Where(x => x.SellerUserId == filter.SellerUserId);
        if (filter.Status.HasValue)
        {
            query = filter.Status == QuoteStatus.Expired
                ? query.Where(x => x.ValidUntil < today && (x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Sent || x.Status == QuoteStatus.Expired))
                : query.Where(x => x.Status == filter.Status && !(x.ValidUntil < today && (x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Sent)));
        }
        var total = await query.CountAsync(ct);
        var ordered = (filter.SortBy, filter.SortDirection == "asc") switch
        {
            ("number", true) => query.OrderBy(x => x.Number).ThenBy(x => x.Id),
            ("number", false) => query.OrderByDescending(x => x.Number).ThenByDescending(x => x.Id),
            ("customer", true) => query.OrderBy(x => x.CustomerName).ThenBy(x => x.Number),
            ("customer", false) => query.OrderByDescending(x => x.CustomerName).ThenByDescending(x => x.Number),
            ("total", true) => query.OrderBy(x => x.Total).ThenBy(x => x.Number),
            ("total", false) => query.OrderByDescending(x => x.Total).ThenByDescending(x => x.Number),
            (_, true) => query.OrderBy(x => x.IssuedAt).ThenBy(x => x.Number),
            _ => query.OrderByDescending(x => x.IssuedAt).ThenByDescending(x => x.Number)
        };
        var page = Math.Max(1, filter.Page); var size = Math.Clamp(filter.PageSize, 10, 100);
        var items = await ordered.Skip((page - 1) * size).Take(size)
            .Select(x => new QuoteListItem(x.Id, x.Number, x.CustomerName, x.IssuedAt, x.ValidUntil,
                x.ValidUntil < today && (x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Sent)
                    ? QuoteStatus.Expired : x.Status, x.Total, x.SellerUserId, x.SaleId)).ToListAsync(ct);
        var all = db.Quotes.AsNoTracking().Where(x => x.CompanyId == companyId);
        var open = await all.CountAsync(x => x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Sent || x.Status == QuoteStatus.Approved, ct);
        var sent = await all.CountAsync(x => x.Status == QuoteStatus.Sent && x.ValidUntil >= today, ct);
        var approved = await all.CountAsync(x => x.Status == QuoteStatus.Approved, ct);
        var expired = await all.CountAsync(x => x.Status == QuoteStatus.Expired || (x.ValidUntil < today && (x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Sent)), ct);
        var converted = await all.CountAsync(x => x.Status == QuoteStatus.Converted, ct);
        var openValue = await all.Where(x => x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Sent || x.Status == QuoteStatus.Approved).SumAsync(x => (decimal?)x.Total, ct) ?? 0;
        var decided = approved + converted + await all.CountAsync(x => x.Status == QuoteStatus.Rejected, ct);
        var rate = decided == 0 ? 0 : Math.Round(converted * 100m / decided, 2);
        return (new(items, page, size, total), new(open, sent, approved, expired, converted, openValue, rate));
    }

    public async Task<(CommercialPage<SaleListItem> Page, SaleSummary Summary)> ListSalesAsync(
        Guid companyId, SaleFilter filter, CancellationToken ct = default)
    {
        var q = db.Sales.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(filter.Search)) { var t = filter.Search.Trim(); q = q.Where(x => EF.Functions.ILike(x.CustomerName, $"%{t}%") || EF.Functions.ILike(x.Number.ToString(), $"%{t}%")); }
        if (filter.CustomerId.HasValue) q = q.Where(x => x.CustomerId == filter.CustomerId);
        if (filter.Status.HasValue) q = q.Where(x => x.Status == filter.Status);
        if (filter.PaymentStatus.HasValue) q = q.Where(x => x.PaymentStatus == filter.PaymentStatus);
        if (filter.FiscalStatus.HasValue) q = q.Where(x => x.FiscalStatus == filter.FiscalStatus);
        if (filter.From.HasValue) q = q.Where(x => x.IssuedAt >= filter.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (filter.To.HasValue) q = q.Where(x => x.IssuedAt < filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var total = await q.CountAsync(ct); var page = Math.Max(1, filter.Page); var size = Math.Clamp(filter.PageSize, 10, 100);
        var ordered = filter.SortDirection == "asc" ? q.OrderBy(x => x.IssuedAt).ThenBy(x => x.Number) : q.OrderByDescending(x => x.IssuedAt).ThenByDescending(x => x.Number);
        var items = await ordered.Skip((page - 1) * size).Take(size).Select(x =>
            new SaleListItem(x.Id, x.Number, x.CustomerName, x.IssuedAt, x.Status, x.PaymentStatus, x.FiscalStatus, x.Total, x.QuoteId)).ToListAsync(ct);
        var all = db.Sales.AsNoTracking().Where(x => x.CompanyId == companyId);
        var summary = new SaleSummary(await all.CountAsync(ct), await all.CountAsync(x => x.Status == SaleStatus.Confirmed, ct),
            await all.CountAsync(x => x.PaymentStatus == PaymentStatus.Pending && x.Status != SaleStatus.Cancelled, ct),
            await all.CountAsync(x => x.Status == SaleStatus.Completed, ct), await all.CountAsync(x => x.Status == SaleStatus.Cancelled, ct),
            await all.Where(x => x.Status != SaleStatus.Cancelled).SumAsync(x => (decimal?)x.Total, ct) ?? 0);
        return (new(items, page, size, total), summary);
    }

    public async Task<CommercialOptions> GetOptionsAsync(Guid companyId, CancellationToken ct = default) => new(
        await db.CommercialPartners.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.Name).Select(x => new CommercialOption(x.Id, x.Name)).ToListAsync(ct),
        await db.PriceTables.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive).OrderByDescending(x => x.IsDefault).ThenBy(x => x.Name).Select(x => new CommercialOption(x.Id, x.Name)).ToListAsync(ct),
        await db.Warehouses.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive).OrderBy(x => x.Name).Select(x => new CommercialOption(x.Id, x.Name)).ToListAsync(ct));

    public async Task<IReadOnlyList<ProductSearchResult>> SearchProductsAsync(Guid companyId, string? search, Guid? priceTableId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var tableId = priceTableId ?? await db.PriceTables.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive && x.IsDefault).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        var q = db.Products.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); q = q.Where(x => EF.Functions.ILike(x.InternalCode, $"%{term}%") || EF.Functions.ILike(x.Name, $"%{term}%")); }
        return await q.OrderBy(x => x.Name).Take(30).Select(x => new ProductSearchResult(x.Id, x.InternalCode, x.Name,
            x.UnitOfMeasure!.Abbreviation,
            db.ProductPrices.Where(p => p.CompanyId == companyId && p.ProductId == x.Id && p.PriceTableId == tableId)
                .Select(p => p.IsPromotionActive && p.PromotionalPrice.HasValue && p.PromotionStartDate <= now && p.PromotionEndDate >= now ? p.PromotionalPrice.Value : p.SalePrice)
                .FirstOrDefault() > 0
                ? db.ProductPrices.Where(p => p.CompanyId == companyId && p.ProductId == x.Id && p.PriceTableId == tableId)
                    .Select(p => p.IsPromotionActive && p.PromotionalPrice.HasValue && p.PromotionStartDate <= now && p.PromotionEndDate >= now ? p.PromotionalPrice.Value : p.SalePrice).First()
                : x.SalePrice,
            x.ControlsStock, x.ProductType != ProductType.ThirdParty, x.DefaultWarehouseId)).ToListAsync(ct);
    }

    public Task<QuoteDetail?> GetQuoteAsync(Guid companyId, Guid id, CancellationToken ct = default) =>
        db.Quotes.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id).Select(x => new QuoteDetail(x.Id, x.Number, x.CustomerId,
            x.CustomerName, x.CustomerDocument, x.IssuedAt, x.ValidUntil, x.Status, x.PriceTableId, x.Notes, x.DeliveryAddress,
            x.Subtotal, x.Discount, x.Freight, x.AdditionalCharges, x.Total, x.SaleId, x.ConcurrencyToken,
            x.Items.OrderBy(i => i.Id).Select(i => new CommercialItemDetail(i.ProductId, i.ProductCode, i.Description, i.Unit, i.Quantity, i.UnitPrice, i.Discount, i.Total, i.ControlsStock, i.IsOwnProduct, i.WarehouseId)).ToList())).FirstOrDefaultAsync(ct);
    public Task<SaleDetail?> GetSaleAsync(Guid companyId, Guid id, CancellationToken ct = default) =>
        db.Sales.AsNoTracking().Where(x => x.CompanyId == companyId && x.Id == id).Select(x => new SaleDetail(x.Id, x.Number, x.CustomerId,
            x.CustomerName, x.CustomerDocument, x.IssuedAt, x.ConfirmedAt, x.Status, x.PaymentStatus, x.FiscalStatus,
            x.Subtotal, x.Discount, x.Freight, x.AdditionalCharges, x.Total, x.Notes, x.DeliveryAddress, x.QuoteId, x.ConcurrencyToken,
            x.Items.OrderBy(i => i.Id).Select(i => new CommercialItemDetail(i.ProductId, i.ProductCode, i.Description, i.Unit, i.Quantity, i.UnitPrice, i.Discount, i.Total, i.ControlsStock, i.IsOwnProduct, i.WarehouseId)).ToList())).FirstOrDefaultAsync(ct);
    public Task<CompanyDocumentHeader?> GetCompanyHeaderAsync(Guid companyId, CancellationToken ct = default) =>
        db.Companies.AsNoTracking().Where(x => x.Id == companyId).Select(x => new CompanyDocumentHeader(x.LegalName, x.TradeName, x.Document)).FirstOrDefaultAsync(ct);

    public async Task<CommercialResult> CreateQuoteAsync(Guid companyId, Guid? userId, SaveQuoteRequest request, bool markSent, CancellationToken ct = default)
    {
        try
        {
            var customer = await db.CommercialPartners.AsNoTracking().FirstOrDefaultAsync(
                x => x.CompanyId == companyId && x.Id == request.CustomerId && x.IsActive, ct);
            if (customer is null) return CommercialResult.Failure("customer_not_found", "Cliente não encontrado.");

            var number = (await db.Quotes.Where(x => x.CompanyId == companyId)
                .MaxAsync(x => (long?)x.Number, ct) ?? 0) + 1;
            var items = await BuildQuoteItems(companyId, request, ct);
            var quote = new Quote(companyId, number, customer.Id, customer.Name, customer.Document, userId,
                DateTimeOffset.UtcNow, request.ValidUntil, request.PriceTableId, request.Notes,
                request.DeliveryAddress) { CreatedBy = userId };
            quote.ReplaceDraft(request.Notes, request.DeliveryAddress, request.ValidUntil, request.Discount,
                request.Freight, request.AdditionalCharges, items);
            if (markSent) quote.MarkSent(DateOnly.FromDateTime(DateTime.UtcNow));
            db.Quotes.Add(quote);
            await db.SaveChangesAsync(ct);
            return CommercialResult.Success(quote.Id);
        }
        catch (ArgumentException ex)
        {
            db.ChangeTracker.Clear();
            return CommercialResult.Failure("validation", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            db.ChangeTracker.Clear();
            logger.LogError(ex, "Falha técnica ao criar orçamento para a empresa {CompanyId}.", companyId);
            return CommercialResult.Failure("persistence_error",
                "Não foi possível salvar o orçamento. Tente novamente; se o problema continuar, contate o suporte.");
        }
    }

    public async Task<CommercialResult> UpdateQuoteAsync(Guid companyId, Guid? userId, Guid id, SaveQuoteRequest request, bool markSent, CancellationToken ct = default)
    {
        try
        {
            return await ExecuteTransactionAsync(async () =>
            {
                var quote = await db.Quotes.FirstOrDefaultAsync(
                    x => x.CompanyId == companyId && x.Id == id, ct);
                if (quote is null) return CommercialResult.Failure("not_found", "Orçamento não encontrado.");
                if (request.ConcurrencyToken != quote.ConcurrencyToken)
                    return CommercialResult.Failure("concurrency_conflict_precheck",
                        "O orçamento foi alterado por outro usuário.");

                var customer = await db.CommercialPartners.AsNoTracking().FirstOrDefaultAsync(x =>
                    x.CompanyId == companyId && x.Id == request.CustomerId && x.IsActive, ct);
                if (customer is null)
                    return CommercialResult.Failure("customer_not_found", "Cliente não encontrado.");
                var items = await BuildQuoteItems(companyId, request, ct);

                // A coleção não é rastreada aqui: a exclusão explícita evita falsos conflitos
                // ao reconstruir o snapshot comercial do rascunho.
                await db.QuoteItems.Where(x => x.CompanyId == companyId && x.QuoteId == id)
                    .ExecuteDeleteAsync(ct);
                quote.ChangeCustomer(customer.Id, customer.Name, customer.Document);
                quote.ReplaceDraft(request.Notes, request.DeliveryAddress, request.ValidUntil,
                    request.Discount, request.Freight, request.AdditionalCharges, items);
                db.QuoteItems.AddRange(items);
                quote.UpdatedBy = userId;
                if (markSent) quote.MarkSent(DateOnly.FromDateTime(DateTime.UtcNow));
                await db.SaveChangesAsync(ct);
                return CommercialResult.Success(quote.Id);
            }, ct);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return CommercialResult.Failure("validation", ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            db.ChangeTracker.Clear();
            logger.LogWarning(ex,
                "Conflito ao atualizar o orçamento {QuoteId} da empresa {CompanyId}. Entidades: {EntityTypes}.",
                id, companyId, string.Join(", ", ex.Entries.Select(x => x.Metadata.ClrType.Name)));
            return CommercialResult.Failure("concurrency_conflict",
                "O orçamento foi alterado por outro usuário. Atualize a página e tente novamente.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            db.ChangeTracker.Clear();
            logger.LogError(ex, "Falha técnica ao atualizar o orçamento {QuoteId} da empresa {CompanyId}.", id, companyId);
            return CommercialResult.Failure("persistence_error",
                "Não foi possível salvar o orçamento. Tente novamente; se o problema continuar, contate o suporte.");
        }
    }

    public async Task<CommercialResult> ChangeQuoteStatusAsync(Guid companyId, Guid? userId, Guid id, QuoteStatus target, CancellationToken ct = default)
    {
        var q = await db.Quotes.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
        if (q is null) return CommercialResult.Failure("not_found", "Orçamento não encontrado.");
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            switch (target) { case QuoteStatus.Sent: q.MarkSent(today); break; case QuoteStatus.Approved: q.Approve(today); break; case QuoteStatus.Rejected: q.Reject(); break; case QuoteStatus.Cancelled: q.Cancel(); break; default: return CommercialResult.Failure("invalid_status", "Situação de destino inválida."); }
            q.UpdatedBy = userId; await db.SaveChangesAsync(ct); return CommercialResult.Success(id);
        }
        catch (InvalidOperationException ex) { return CommercialResult.Failure("invalid_transition", ex.Message); }
        catch (DbUpdateConcurrencyException ex)
        {
            db.ChangeTracker.Clear();
            logger.LogWarning(ex, "Conflito ao alterar o orçamento {QuoteId} da empresa {CompanyId}.", id, companyId);
            return CommercialResult.Failure("concurrency_conflict",
                "O orçamento foi alterado por outro usuário. Atualize a página e tente novamente.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            db.ChangeTracker.Clear();
            logger.LogError(ex, "Falha técnica ao alterar o orçamento {QuoteId} da empresa {CompanyId}.", id, companyId);
            return CommercialResult.Failure("persistence_error",
                "Não foi possível atualizar o orçamento. Tente novamente.");
        }
    }

    public async Task<CommercialResult> ConvertQuoteAsync(Guid companyId, Guid? userId, Guid id, CancellationToken ct = default)
    {
        try
        {
            return await ExecuteTransactionAsync(async () =>
            {
                var q = await db.Quotes.Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (q is null) return CommercialResult.Failure("not_found", "Orçamento não encontrado.");
                if (q.SaleId.HasValue) return CommercialResult.Success(q.SaleId.Value);
                if (q.Status != QuoteStatus.Approved)
                    return CommercialResult.Failure("not_approved", "Somente um orçamento aprovado pode ser convertido.");

                var number = (await db.Sales.Where(x => x.CompanyId == companyId)
                    .MaxAsync(x => (long?)x.Number, ct) ?? 0) + 1;
                var saleItems = q.Items.Select(x => new SaleItem(companyId, x.ProductId, x.ProductCode,
                    x.Description, x.Unit, x.Quantity, x.UnitPrice, x.Discount, x.Total, x.IsOwnProduct,
                    x.ControlsStock, x.WarehouseId)).ToList();
                var sale = new Sale(companyId, number, q.CustomerId, q.CustomerName, q.CustomerDocument,
                    q.Id, q.SellerUserId, DateTimeOffset.UtcNow, q.Subtotal, q.Discount, q.Freight,
                    q.AdditionalCharges, q.Notes, q.DeliveryAddress, saleItems) { CreatedBy = userId };
                q.MarkConverted(sale.Id);
                q.UpdatedBy = userId;
                db.Sales.Add(sale);
                await db.SaveChangesAsync(ct);
                return CommercialResult.Success(sale.Id);
            }, ct);
        }
        catch (DbUpdateException ex)
        {
            db.ChangeTracker.Clear();
            logger.LogWarning(ex, "Conflito ao converter o orçamento {QuoteId} da empresa {CompanyId}.", id, companyId);
            var existing = await db.Sales.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.QuoteId == id)
                .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
            return existing.HasValue
                ? CommercialResult.Success(existing.Value)
                : CommercialResult.Failure("conversion_conflict",
                    "Não foi possível converter o orçamento agora. Atualize a página e tente novamente.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            db.ChangeTracker.Clear();
            logger.LogError(ex, "Falha técnica ao converter o orçamento {QuoteId} da empresa {CompanyId}.", id, companyId);
            return CommercialResult.Failure("persistence_error",
                "Não foi possível converter o orçamento. Tente novamente.");
        }
    }

    public async Task<CommercialResult> ConfirmSaleAsync(Guid companyId, Guid? userId, Guid id, CancellationToken ct = default)
    {
        try
        {
            return await ExecuteTransactionAsync(async () =>
            {
                var sale = await db.Sales.Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
                if (sale is null) return CommercialResult.Failure("not_found", "Venda não encontrada.");
                if (sale.Status == SaleStatus.Confirmed) return CommercialResult.Success(id);

                var controlled = sale.Items.Where(x => x.ControlsStock && x.IsOwnProduct).ToList();
                if (controlled.Any(x => !x.WarehouseId.HasValue))
                    return CommercialResult.Failure("warehouse_required",
                        "Defina um depósito para todos os produtos controlados.");
                var stock = await stockService.RegisterStockIssueBatchAsync(companyId, userId,
                    new(controlled.Select(x => new RegisterStockMovementRequest(x.ProductId,
                        x.WarehouseId!.Value, x.Quantity, $"Confirmação da venda VEN-{sale.Number:000000}",
                        ReferenceType: "Sale", ReferenceId: sale.Id.ToString(),
                        DocumentNumber: $"VEN-{sale.Number:000000}",
                        OperationKey: $"sale:{sale.Id}:item:{x.Id}")).ToList()), ct);
                if (!stock.Succeeded)
                    return CommercialResult.Failure(stock.ErrorCode ?? "stock_error",
                        stock.ErrorMessage ?? "Não foi possível baixar o estoque.");

                sale.Confirm(DateTimeOffset.UtcNow);
                sale.UpdatedBy = userId;
                await db.SaveChangesAsync(ct);
                return CommercialResult.Success(id);
            }, ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            db.ChangeTracker.Clear();
            logger.LogWarning(ex, "Conflito ao confirmar a venda {SaleId} da empresa {CompanyId}.", id, companyId);
            return CommercialResult.Failure("concurrency_conflict",
                "A venda foi alterada durante a confirmação. Atualize a página e tente novamente.");
        }
        catch (DbUpdateException ex)
        {
            db.ChangeTracker.Clear();
            logger.LogError(ex, "Falha técnica ao confirmar a venda {SaleId} da empresa {CompanyId}.", id, companyId);
            return CommercialResult.Failure("persistence_error",
                "Não foi possível confirmar a venda. Nenhuma baixa de estoque foi concluída.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            db.ChangeTracker.Clear();
            logger.LogError(ex, "Falha técnica ao confirmar a venda {SaleId} da empresa {CompanyId}.", id, companyId);
            return CommercialResult.Failure("persistence_error",
                "Não foi possível confirmar a venda. Nenhuma baixa de estoque foi concluída.");
        }
    }

    public async Task<CommercialResult> CancelSaleAsync(Guid companyId, Guid? userId, Guid id, CancellationToken ct = default)
    {
        var sale = await db.Sales.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, ct);
        if (sale is null) return CommercialResult.Failure("not_found", "Venda não encontrada.");
        try { sale.Cancel(DateTimeOffset.UtcNow); sale.UpdatedBy = userId; await db.SaveChangesAsync(ct); return CommercialResult.Success(id); }
        catch (InvalidOperationException ex) { return CommercialResult.Failure("invalid_transition", ex.Message); }
    }

    private async Task<List<QuoteItem>> BuildQuoteItems(Guid companyId, SaveQuoteRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0) throw new InvalidOperationException("Inclua ao menos um item.");
        var ids = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive && ids.Contains(x.Id))
            .Select(x => new { x.Id, x.InternalCode, x.Name, Unit = x.UnitOfMeasure!.Abbreviation, x.SalePrice, x.ControlsStock, x.ProductType, x.DefaultWarehouseId }).ToDictionaryAsync(x => x.Id, ct);
        if (products.Count != ids.Count) throw new InvalidOperationException("Um ou mais produtos não pertencem à empresa atual.");
        var priceResults = await SearchProductsAsync(companyId, null, request.PriceTableId, ct);
        var prices = priceResults.Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.UnitPrice);
        var items = new List<QuoteItem>();
        foreach (var input in request.Items)
        {
            var p = products[input.ProductId]; var price = input.UnitPrice ?? prices.GetValueOrDefault(p.Id, p.SalePrice);
            var warehouse = input.WarehouseId ?? p.DefaultWarehouseId;
            if (p.ControlsStock && warehouse.HasValue && !await db.Warehouses.AsNoTracking().AnyAsync(x => x.CompanyId == companyId && x.Id == warehouse, ct))
                throw new InvalidOperationException("O depósito informado não pertence à empresa atual.");
            items.Add(new QuoteItem(companyId, p.Id, p.InternalCode, p.Name, p.Unit, input.Quantity, price, input.Discount,
                p.ProductType != ProductType.ThirdParty, p.ControlsStock, warehouse));
        }
        return items;
    }

    private async Task<CommercialResult> ExecuteTransactionAsync(
        Func<Task<CommercialResult>> operation,
        CancellationToken ct)
    {
        CommercialResult? result = null;
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // A estratégia pode repetir o delegate. Nada rastreado por uma tentativa anterior é reutilizado.
            db.ChangeTracker.Clear();
            await using var transaction =
                await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                result = await operation();
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync(ct);
                    return;
                }

                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
        return result ?? CommercialResult.Failure("persistence_error",
            "Não foi possível concluir a operação. Tente novamente.");
    }
}

public sealed class DisabledFiscalDocumentService : IFiscalDocumentService
{
    public Task<FiscalDocumentResult> RequestAsync(FiscalDocumentRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new FiscalDocumentResult(false, FiscalDocumentStatus.NotRequested,
            "O módulo fiscal não está habilitado nesta versão."));
}
