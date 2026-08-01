using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;
namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class RollbackImportacaoService(ApplicationDbContext db, ILogger<RollbackImportacaoService> logger) : IRollbackImportacaoService
{
    public async Task<AnaliseRollbackImportacao> AnalisarAsync(Guid id, Guid company, Guid? user, CancellationToken ct = default)
    {
        await Authorize(company, user, ct); var h = await db.ImportacoesHistorico.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == company, ct) ?? throw new RollbackImportacaoException("Importação não encontrada."); if (h.Status is StatusImportacao.Revertida or StatusImportacao.RevertidaParcialmente or StatusImportacao.RollbackFalhou or StatusImportacao.RollbackEmAndamento) return new(id, false, 0, 0, 0, TimeSpan.Zero, ["A importação já foi revertida ou possui rollback em andamento."]); if (h.Status is not (StatusImportacao.Concluida or StatusImportacao.ConcluidaParcialmente)) return new(id, false, 0, 0, 0, TimeSpan.Zero, ["Somente importações concluídas permitem rollback."]);
        var items = await db.ImportacaoItens.AsNoTracking().Where(x => x.CompanyId == company && x.ImportacaoHistoricoId == id && x.ProdutoId.HasValue && (x.Status == StatusLinhaImportacao.Inserida || x.Status == StatusLinhaImportacao.Atualizada)).Select(x => new { x.ProdutoId, x.Status, x.ExecutadoEm }).ToListAsync(ct); var ids = items.Select(x => x.ProdutoId!.Value).Distinct().ToList(); var origins = $"importação:{id}"; var executionEnd = h.FinalizadoEm ?? h.CreatedAt; var later = await db.ProductChangeHistories.AsNoTracking().Where(x => x.CompanyId == company && ids.Contains(x.ProductId) && x.CreatedAt > executionEnd && x.Origin != origins && x.Origin != $"rollback:{id}").Select(x => x.ProductId).Distinct().ToListAsync(ct); var blocked = items.Count(x => later.Contains(x.ProdutoId!.Value)); var risks = new List<string> { "Produtos criados serão removidos logicamente.", "Produtos atualizados serão restaurados pelos snapshots de auditoria." }; if (blocked > 0) risks.Add($"{blocked} produto(s) possuem alterações posteriores e serão bloqueados."); return new(id, items.Count > blocked, items.Count(x => x.Status == StatusLinhaImportacao.Inserida), items.Count(x => x.Status == StatusLinhaImportacao.Atualizada), blocked, TimeSpan.FromMilliseconds(items.Count * 4), risks);
    }
    public async Task<ResultadoRollbackImportacao> ExecutarAsync(Guid id, Guid company, Guid? user, bool partial, CancellationToken ct = default)
    {
        var analysis = await AnalisarAsync(id, company, user, ct); if (!analysis.Permitido) throw new RollbackImportacaoException(string.Join(" ", analysis.Riscos)); if (!partial && analysis.ProdutosBloqueados > 0) throw new RollbackImportacaoException("Existem produtos com uso ou alterações posteriores; rollback integral cancelado."); var start = DateTimeOffset.UtcNow;
        try
        {
            var strategy = db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    var h = await db.ImportacoesHistorico.FirstAsync(x => x.Id == id && x.CompanyId == company, ct); var executionEnd = h.FinalizadoEm ?? h.CreatedAt; h.IniciarRollback(user); h.UpdatedBy = user; await db.SaveChangesAsync(ct); var items = await db.ImportacaoItens.Where(x => x.CompanyId == company && x.ImportacaoHistoricoId == id && x.ProdutoId.HasValue && (x.Status == StatusLinhaImportacao.Inserida || x.Status == StatusLinhaImportacao.Atualizada)).OrderBy(x => x.NumeroLinha).ToListAsync(ct); var ids = items.Select(x => x.ProdutoId!.Value).Distinct().ToList(); var products = await db.Products.IgnoreQueryFilters().Where(x => x.CompanyId == company && ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct); var origin = $"importação:{id}"; var later = (await db.ProductChangeHistories.AsNoTracking().Where(x => x.CompanyId == company && ids.Contains(x.ProductId) && x.CreatedAt > executionEnd && x.Origin != origin && x.Origin != $"rollback:{id}").Select(x => x.ProductId).Distinct().ToListAsync(ct)).ToHashSet(); var removed = 0; var restored = 0; var blocked = 0; var failures = 0; var results = new List<ResultadoRollbackItem>();
                    var stockOperationKeys = items.SelectMany(item => new[] { ImportStockOperationKey(id, item.Id), RollbackStockOperationKey(id, item.Id) }).ToList();
                    var stockMovements = await db.StockMovements.AsNoTracking().Where(x => x.CompanyId == company && x.OperationKey != null && stockOperationKeys.Contains(x.OperationKey)).ToListAsync(ct);
                    var initialStockMovements = stockMovements.Where(x => x.Type == StockMovementType.InitialBalance).ToDictionary(x => x.OperationKey!, StringComparer.Ordinal);
                    var existingRollbackStockKeys = stockMovements.Where(x => x.Type == StockMovementType.NegativeAdjustment).Select(x => x.OperationKey!).ToHashSet(StringComparer.Ordinal);
                    var stockBalanceIds = initialStockMovements.Values.Select(x => x.StockBalanceId).Distinct().ToList();
                    var stockBalances = await db.StockBalances.Where(x => x.CompanyId == company && stockBalanceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
                    foreach (var item in items)
                    {
                        ct.ThrowIfCancellationRequested(); var pid = item.ProdutoId!.Value;
                        item.UpdatedBy = user;
                        if (later.Contains(pid)) { item.RegistrarRollback(false, "Bloqueado por alterações posteriores."); blocked++; results.Add(new(item.NumeroLinha, pid, "Bloquear", false, item.MensagemRollback!)); continue; }
                        if (!products.TryGetValue(pid, out var p)) { item.RegistrarRollback(false, "Produto não encontrado."); failures++; results.Add(new(item.NumeroLinha, pid, "Falha", false, item.MensagemRollback!)); if (!partial) throw new RollbackImportacaoException(item.MensagemRollback!); continue; }
                        var savepoint = $"rollback_item_{item.Id:N}";
                        StockBalance? stockBalance = null;
                        StockMovement? inverseStockMovement = null;
                        if (partial) await tx.CreateSavepointAsync(savepoint, ct);
                        try
                        {
                            initialStockMovements.TryGetValue(ImportStockOperationKey(id, item.Id), out var initialStockMovement);
                            stockBalances.TryGetValue(initialStockMovement?.StockBalanceId ?? Guid.Empty, out stockBalance);
                            inverseStockMovement = ReverseInitialStock(id, item, initialStockMovement, stockBalance, existingRollbackStockKeys, user);
                            if (inverseStockMovement is not null)
                            {
                                stockBalance!.UpdatedBy = user;
                                db.StockMovements.Add(inverseStockMovement);
                            }
                            if (item.Status == StatusLinhaImportacao.Inserida) { p.DeletedBy = user; db.Products.Remove(p); removed++; item.RegistrarRollback(true, "Produto removido logicamente."); db.ProductChangeHistories.Add(new(company, p.Id, "Produto", "Ativo", "Excluído", $"rollback:{id}") { CreatedBy = user }); results.Add(new(item.NumeroLinha, pid, "Remover", true, item.MensagemRollback!)); }
                            else { Restore(p, item.AlteracoesAplicadasJson); p.UpdatedBy = user; restored++; item.RegistrarRollback(true, "Valores anteriores restaurados."); db.ProductChangeHistories.Add(new(company, p.Id, "Produto", "Importado", "Restaurado", $"rollback:{id}") { CreatedBy = user }); results.Add(new(item.NumeroLinha, pid, "Restaurar", true, item.MensagemRollback!)); }
                            if (partial)
                            {
                                await db.SaveChangesAsync(ct);
                                await tx.ReleaseSavepointAsync(savepoint, ct);
                            }
                        }
                        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or JsonException or RollbackImportacaoException)
                        {
                            logger.LogWarning(ex, "Rollback bloqueado para produto {Product}", pid);
                            if (!partial) throw;
                            await tx.RollbackToSavepointAsync(savepoint, ct);
                            await ReloadFailedPartialItemAsync(id, item, p, stockBalance, company, stockBalances,
                                initialStockMovements, existingRollbackStockKeys, ct);
                            item.RegistrarRollback(false, ex.Message);
                            failures++;
                            results.Add(new(item.NumeroLinha, pid, "Falha", false, ex.Message));
                            await db.SaveChangesAsync(ct);
                            await tx.ReleaseSavepointAsync(savepoint, ct);
                        }
                    }
                    h.FinalizarRollback(removed, restored, blocked, failures, $"Rollback {(partial ? "parcial" : "integral")} executado.");
                    h.UpdatedBy = user;
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return new ResultadoRollbackImportacao(id, removed, restored, blocked, failures, start, h.RollbackFinalizadoEm!.Value, h.Status, [], results);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no rollback {ImportId}", id);
            var failureStrategy = db.Database.CreateExecutionStrategy();
            await failureStrategy.ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                var h = await db.ImportacoesHistorico.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == company, ct);
                if (h is not null && h.Status is StatusImportacao.Concluida or StatusImportacao.ConcluidaParcialmente)
                {
                    h.IniciarRollback(user);
                    h.UpdatedBy = user;
                }
                if (h is not null && h.Status == StatusImportacao.RollbackEmAndamento)
                {
                    h.FinalizarRollback(0, 0, 0, 1, "Rollback revertido por falha transacional.");
                    h.UpdatedBy = user;
                    await db.SaveChangesAsync(ct);
                }
            });
            throw new RollbackImportacaoException("Não foi possível concluir o rollback com segurança.");
        }
    }
    public async Task<ResultadoRollbackImportacao?> ObterResultadoAsync(Guid id, Guid company, CancellationToken ct = default) { var h = await db.ImportacoesHistorico.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == company, ct); if (h is null || h.RollbackFinalizadoEm is null) return null; var items = await db.ImportacaoItens.AsNoTracking().Where(x => x.CompanyId == company && x.ImportacaoHistoricoId == id && x.RollbackExecutadoEm.HasValue).OrderBy(x => x.NumeroLinha).Select(x => new ResultadoRollbackItem(x.NumeroLinha, x.ProdutoId, x.Status == StatusLinhaImportacao.Inserida ? "Remover" : "Restaurar", x.Revertido, x.MensagemRollback ?? "")).ToListAsync(ct); return new(id, h.ProdutosRemovidosRollback, h.ProdutosRestauradosRollback, h.ProdutosBloqueadosRollback, h.FalhasRollback, h.RollbackIniciadoEm ?? h.CreatedAt, h.RollbackFinalizadoEm.Value, h.Status, string.IsNullOrWhiteSpace(h.ObservacoesRollback) ? [] : [h.ObservacoesRollback], items); }
    public async Task<ArquivoExportacaoImportacao> ExportarExcelAsync(Guid id, Guid company, CancellationToken ct = default) { var r = await ObterResultadoAsync(id, company, ct) ?? throw new KeyNotFoundException(); using var wb = new XLWorkbook(); var ws = wb.AddWorksheet("Rollback"); var headers = new[] { "Linha", "Produto", "Operação", "Revertido", "Mensagem" }; for (var c = 0; c < headers.Length; c++) { ws.Cell(1, c + 1).Value = headers[c]; ws.Cell(1, c + 1).Style.Font.Bold = true; } for (var n = 0; n < r.Itens.Count; n++) { var i = r.Itens[n]; ws.Cell(n + 2, 1).Value = i.Linha; ws.Cell(n + 2, 2).Value = i.ProdutoId?.ToString() ?? ""; ws.Cell(n + 2, 3).Value = i.Operacao; ws.Cell(n + 2, 4).Value = i.Revertido ? "Sim" : "Não"; ws.Cell(n + 2, 5).Value = i.Mensagem; } ws.SheetView.FreezeRows(1); ws.RangeUsed()?.SetAutoFilter(); ws.Columns().AdjustToContents(); using var ms = new MemoryStream(); wb.SaveAs(ms); return new(ms.ToArray(), $"rollback-{id:N}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"); }
    public async Task<ArquivoExportacaoImportacao> ExportarCsvAsync(Guid id, Guid company, CancellationToken ct = default) { var r = await ObterResultadoAsync(id, company, ct) ?? throw new KeyNotFoundException(); using var ms = new MemoryStream(); using (var w = new StreamWriter(ms, new UTF8Encoding(true), leaveOpen: true)) { w.WriteLine("Linha;Produto;Operação;Revertido;Mensagem"); foreach (var i in r.Itens) w.WriteLine($"{i.Linha};{i.ProdutoId};{Csv(i.Operacao)};{(i.Revertido ? "Sim" : "Não")};{Csv(i.Mensagem)}"); } return new(ms.ToArray(), $"rollback-{id:N}.csv", "text/csv; charset=utf-8"); }
    private async Task Authorize(Guid company, Guid? user, CancellationToken ct) { if (user is null || !await db.Users.AsNoTracking().AnyAsync(x => x.Id == user && x.CompanyId == company && x.IsActive, ct)) { logger.LogWarning("Tentativa de rollback não autorizada. Empresa {CompanyId}, usuário {UserId}", company, user); throw new UnauthorizedAccessException(); } }
    private static string Csv(string s) => "\"" + s.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
    private static StockMovement? ReverseInitialStock(Guid importId, ImportacaoItem item, StockMovement? initialMovement, StockBalance? balance, HashSet<string> existingRollbackKeys, Guid? user)
    {
        if (initialMovement is null) return null;
        var rollbackKey = RollbackStockOperationKey(importId, item.Id);
        if (existingRollbackKeys.Contains(rollbackKey)) return null;
        if (balance is null) throw new RollbackImportacaoException("Saldo do estoque inicial não encontrado.");
        var movement = balance.Apply(StockMovementType.NegativeAdjustment, initialMovement.Quantity,
            "Rollback de importação — estorno do estoque inicial", initialMovement.InternalLocationId,
            $"Estorno do estoque inicial da linha {item.NumeroLinha}.", initialMovement.UnitCost,
            "ImportacaoHistorico", importId.ToString(), null, rollbackKey, user);
        existingRollbackKeys.Add(rollbackKey);
        return movement;
    }
    private static string ImportStockOperationKey(Guid importId, Guid itemId) => $"importacao:{importId:N}:{itemId:N}:estoque-inicial";
    private static string RollbackStockOperationKey(Guid importId, Guid itemId) => $"rollback:{importId:N}:{itemId:N}:estoque-inicial";
    private async Task ReloadFailedPartialItemAsync(
        Guid importId,
        ImportacaoItem item,
        Product product,
        StockBalance? balance,
        Guid company,
        Dictionary<Guid, StockBalance> stockBalances,
        Dictionary<string, StockMovement> initialStockMovements,
        HashSet<string> existingRollbackStockKeys,
        CancellationToken ct)
    {
        var importKey = ImportStockOperationKey(importId, item.Id);
        var rollbackKey = RollbackStockOperationKey(importId, item.Id);
        var operationKeys = new[] { importKey, rollbackKey };

        foreach (var entry in db.ChangeTracker.Entries<StockMovement>()
                     .Where(entry => entry.Entity.OperationKey != null && operationKeys.Contains(entry.Entity.OperationKey)))
            entry.State = EntityState.Detached;
        foreach (var entry in db.ChangeTracker.Entries<ProductChangeHistory>()
                     .Where(entry => entry.State == EntityState.Added && entry.Entity.ProductId == product.Id && entry.Entity.Origin == $"rollback:{importId}"))
            entry.State = EntityState.Detached;

        await db.Entry(product).ReloadAsync(ct);
        if (balance is not null)
        {
            var balanceId = balance.Id;
            db.Entry(balance).State = EntityState.Detached;
            var reloadedBalance = await db.StockBalances.FirstAsync(
                current => current.CompanyId == company && current.Id == balanceId, ct);
            stockBalances[balanceId] = reloadedBalance;
        }

        var reloadedMovements = await db.StockMovements.AsNoTracking()
            .Where(movement => movement.CompanyId == company && movement.OperationKey != null && operationKeys.Contains(movement.OperationKey))
            .ToListAsync(ct);
        initialStockMovements.Remove(importKey);
        var initialMovement = reloadedMovements.FirstOrDefault(movement => movement.OperationKey == importKey && movement.Type == StockMovementType.InitialBalance);
        if (initialMovement is not null) initialStockMovements[importKey] = initialMovement;
        existingRollbackStockKeys.Remove(rollbackKey);
        if (reloadedMovements.Any(movement => movement.OperationKey == rollbackKey && movement.Type == StockMovementType.NegativeAdjustment))
            existingRollbackStockKeys.Add(rollbackKey);
    }
    private static void Restore(Product p, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new RollbackImportacaoException("Snapshot de alterações indisponível.");

        var changes = JsonSerializer.Deserialize<Dictionary<string, RollbackChange>>(json) ?? throw new JsonException();
        string? Old(string key, string? fallback) => changes.TryGetValue(key, out var change) ? change.ValueString() : fallback;

        var unit = ParseGuid(Old("Unidade", p.UnitOfMeasureId.ToString()), p.UnitOfMeasureId);
        var type = ParseEnum(Old("Tipo", p.ProductType.ToString()), p.ProductType);
        var category = ParseNullableGuid(Old("Categoria", p.CategoryId?.ToString()), p.CategoryId);
        var subcategory = ParseNullableGuid(Old("Subcategoria", p.SubcategoryId?.ToString()), p.SubcategoryId);
        var brand = ParseNullableGuid(Old("Marca", p.BrandId?.ToString()), p.BrandId);
        var group = ParseNullableGuid(Old("Grupo", p.ProductGroupId?.ToString()), p.ProductGroupId);
        var supplier = ParseNullableGuid(Old("Fornecedor", p.MainSupplierId?.ToString()), p.MainSupplierId);
        var partner = ParseNullableGuid(Old("Parceiro", p.PartnerId?.ToString()), p.PartnerId);
        var defaultWarehouse = ParseNullableGuid(Old("Depósito padrão", p.DefaultWarehouseId?.ToString()), p.DefaultWarehouseId);
        var defaultWarehouseLocation = ParseNullableGuid(Old("Local interno padrão", p.DefaultWarehouseLocationId?.ToString()), p.DefaultWarehouseLocationId);
        var minimumStock = ParseNullableDecimal(Old("Estoque mínimo", p.MinimumStock?.ToString()), p.MinimumStock);
        var controlsStock = ParseBool(Old("Controla estoque", p.ControlsStock.ToString()), p.ControlsStock);
        var isActive = ParseBool(Old("Status", p.IsActive.ToString()), p.IsActive);
        var cost = ParseDecimal(Old("Preço de custo", p.CostPrice.ToString()), p.CostPrice);
        var sale = ParseDecimal(Old("Preço de venda", p.SalePrice.ToString()), p.SalePrice);

        p.Update(
            p.InternalCode,
            p.Sku,
            Old("Código de barras", p.Barcode),
            p.Reference,
            Old("Descrição", p.Name)!,
            p.ShortDescription,
            Old("Descrição complementar", p.Description),
            type,
            controlsStock,
            isActive,
            category,
            subcategory,
            brand,
            unit,
            group,
            supplier,
            partner,
            defaultWarehouse,
            defaultWarehouseLocation,
            Old("NCM", p.Ncm),
            p.Cest,
            cost,
            sale,
            p.CommissionType,
            p.CommissionValue,
            p.PriceValidUntil,
            minimumStock,
            Old("Observações", p.Notes));
    }
    private static Guid ParseGuid(string? value, Guid fallback) => Guid.TryParse(value, out var parsed) ? parsed : fallback;
    private static Guid? ParseNullableGuid(string? value, Guid? fallback) => string.IsNullOrWhiteSpace(value) ? null : Guid.TryParse(value, out var parsed) ? parsed : fallback;
    private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum => Enum.TryParse<T>(value, out var parsed) ? parsed : fallback;
    private static bool ParseBool(string? value, bool fallback) => bool.TryParse(value, out var parsed) ? parsed : fallback;
    private static decimal ParseDecimal(string? value, decimal fallback) => decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var x) || decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out x) ? x : fallback;
    private static decimal? ParseNullableDecimal(string? value, decimal? fallback) => string.IsNullOrWhiteSpace(value) ? null : decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var x) || decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out x) ? x : fallback;
    private sealed record RollbackChange(JsonElement Value, JsonElement Novo) { public string? ValueString() => Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : Value.ToString(); }
}
