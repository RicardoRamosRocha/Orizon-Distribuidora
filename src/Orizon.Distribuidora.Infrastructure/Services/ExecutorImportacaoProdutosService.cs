using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class ExecutorImportacaoProdutosService(ApplicationDbContext db, ILogger<ExecutorImportacaoProdutosService> logger) : IExecutorImportacaoProdutosService
{
    private const int BatchSize = 200;

    public async Task<ResultadoExecucaoImportacao> ExecutarAsync(Guid importacaoId, Guid empresaId, Guid? usuarioId, CancellationToken ct = default)
    {
        var inicio = DateTimeOffset.UtcNow;
        await AcquireExecutionAsync(importacaoId, empresaId, usuarioId, ct);
        try
        {
            var history = await db.ImportacoesHistorico.AsNoTracking().FirstAsync(x => x.Id == importacaoId && x.CompanyId == empresaId, ct);
            var options = JsonSerializer.Deserialize<OpcoesValidacaoImportacao>(history.OpcoesValidacaoJson!) ?? new();
            if (!options.PermitirImportacaoParcial && await db.ImportacaoItens.AsNoTracking().AnyAsync(x => x.CompanyId == empresaId && x.ImportacaoHistoricoId == importacaoId && x.Status == StatusLinhaImportacao.ComErro, ct))
            {
                await CompleteHistoryAsync(importacaoId, empresaId, ct); return (await ObterResultadoAsync(importacaoId, empresaId, ct))!;
            }

            var lastLine = 0;
            while (true)
            {
                var lineNumbers = await db.ImportacaoItens.AsNoTracking().Where(x => x.CompanyId == empresaId && x.ImportacaoHistoricoId == importacaoId && x.NumeroLinha > lastLine)
                    .OrderBy(x => x.NumeroLinha).Select(x => x.NumeroLinha).Take(options.PermitirImportacaoParcial ? BatchSize : 10_000).ToListAsync(ct);
                if (lineNumbers.Count == 0) break; lastLine = lineNumbers[^1];
                await ProcessBatchAsync(importacaoId, empresaId, usuarioId, options, lineNumbers, ct);
                if (!options.PermitirImportacaoParcial) break;
            }
            await CompleteHistoryAsync(importacaoId, empresaId, ct);
            return (await ObterResultadoAsync(importacaoId, empresaId, ct))!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha geral na importação {ImportId}", importacaoId);
            db.ChangeTracker.Clear(); await CompleteHistoryAsync(importacaoId, empresaId, ct, fatalFailure: true);
            throw ex is ImportacaoExecucaoException ? ex : new ImportacaoExecucaoException("Não foi possível concluir a importação. Os lotes já confirmados foram preservados e os contadores recalculados.");
        }
    }

    private async Task AcquireExecutionAsync(Guid id, Guid company, Guid? user, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var history = await db.ImportacoesHistorico.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == company, ct) ?? throw new ImportacaoExecucaoException("Importação não encontrada para a empresa atual.");
                if (history.Status is StatusImportacao.Concluida or StatusImportacao.ConcluidaParcialmente) throw new ImportacaoExecucaoException("Esta importação já foi executada.");
                if (history.Status == StatusImportacao.Importando) throw new ImportacaoExecucaoException("Esta importação já está sendo executada.");
                if (string.IsNullOrWhiteSpace(history.OpcoesValidacaoJson)) throw new ImportacaoExecucaoException("As opções validadas da importação não estão disponíveis.");
                history.IniciarExecucao(user);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
        db.ChangeTracker.Clear();
    }

    private async Task ProcessBatchAsync(Guid importId, Guid company, Guid? user, OpcoesValidacaoImportacao options, IReadOnlyList<int> lines, CancellationToken ct)
    {
        try
        {
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    var items = await db.ImportacaoItens.Where(x => x.CompanyId == company && x.ImportacaoHistoricoId == importId && lines.Contains(x.NumeroLinha)).OrderBy(x => x.NumeroLinha).ToListAsync(ct);
                    var data = items.Where(x => x.Status == StatusLinhaImportacao.Valida && !string.IsNullOrWhiteSpace(x.DadosNormalizadosJson)).ToDictionary(x => x.Id, Parse);
                    var codes = data.Values.Select(x => Text(x, "codigo")).Where(x => x is not null).Distinct().ToList();
                    var barcodes = data.Values.Select(x => Text(x, "codigoBarras")).Where(x => x is not null).Distinct().ToList();
                    var products = await db.Products.Where(x => x.CompanyId == company && codes.Contains(x.InternalCode)).ToDictionaryAsync(x => x.InternalCode, StringComparer.Ordinal, ct);
                    var barcodeOwners = await db.Products.AsNoTracking().Where(x => x.CompanyId == company && x.Barcode != null && barcodes.Contains(x.Barcode)).ToDictionaryAsync(x => x.Barcode!, x => x.Id, StringComparer.Ordinal, ct);
                    var existingProductIds = products.Values.Select(x => x.Id).ToList();
                    var balances = options.DepositoId.HasValue ? await db.StockBalances.Where(x => x.CompanyId == company && x.WarehouseId == options.DepositoId && existingProductIds.Contains(x.ProductId)).ToDictionaryAsync(x => x.ProductId, ct) : [];
                    var initialProducts = options.DepositoId.HasValue ? (await db.StockMovements.AsNoTracking().Where(x => x.CompanyId == company && x.WarehouseId == options.DepositoId && existingProductIds.Contains(x.ProductId) && x.Type == StockMovementType.InitialBalance).Select(x => x.ProductId).Distinct().ToListAsync(ct)).ToHashSet() : [];
                    var expectedOperationKeys = data.Keys.Select(itemId => StockOperationKey(importId, itemId)).ToList();
                    var existingOperationKeys = options.DepositoId.HasValue
                        ? (await db.StockMovements.AsNoTracking().Where(x => x.CompanyId == company && x.OperationKey != null && expectedOperationKeys.Contains(x.OperationKey)).Select(x => x.OperationKey!).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal)
                        : [];

                    foreach (var item in items)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (item.Status == StatusLinhaImportacao.ComErro) { item.PrepararExecucao(OperacaoExecucaoImportacao.Bloquear); item.ConcluirExecucao(StatusLinhaImportacao.Bloqueada, null, "Linha bloqueada pela validação."); continue; }
                        if (item.Status == StatusLinhaImportacao.Ignorada) { item.PrepararExecucao(OperacaoExecucaoImportacao.Ignorar); item.ConcluirExecucao(StatusLinhaImportacao.Ignorada, null, "Linha ignorada."); continue; }
                        if (!data.TryGetValue(item.Id, out var row)) { item.PrepararExecucao(OperacaoExecucaoImportacao.Bloquear); item.ConcluirExecucao(StatusLinhaImportacao.Bloqueada, null, "Dados validados indisponíveis."); continue; }
                        var code = Text(row, "codigo")!; var name = Text(row, "descricao")!; var barcode = Text(row, "codigoBarras"); products.TryGetValue(code, out var product);
                        var inserting = product is null; item.PrepararExecucao(inserting ? OperacaoExecucaoImportacao.Inserir : OperacaoExecucaoImportacao.Atualizar);
                        if (inserting && !options.InserirNovos) { item.ConcluirExecucao(StatusLinhaImportacao.Ignorada, null, "Inclusão de novos desativada."); continue; }
                        if (!inserting && !options.AtualizarExistentes) { item.ConcluirExecucao(StatusLinhaImportacao.Ignorada, product!.Id, "Atualização desativada."); continue; }
                        if (barcode is not null && barcodeOwners.TryGetValue(barcode, out var owner) && owner != product?.Id) throw new ImportacaoExecucaoException($"Linha {item.NumeroLinha}: código de barras já utilizado.");
                        if (inserting)
                        {
                            var unit = GuidValue(row, "unidadeId"); if (unit == Guid.Empty) throw new ImportacaoExecucaoException($"Linha {item.NumeroLinha}: unidade inválida.");
                            product = new Product(company, code, name, unit, ProductTypeValue(row)); product.CreatedBy = user; db.Products.Add(product); products[code] = product;
                        }
                        var before = inserting ? null : Snapshot(product!); Apply(product!, row, options, options.IgnorarVaziosAtualizacao); var after = Snapshot(product!);
                        var changes = before is null ? [] : before.Where(x => after[x.Key] != x.Value).ToDictionary(x => x.Key, x => new { x.Value, Novo = after[x.Key] });
                        if (inserting) db.ProductChangeHistories.Add(new(company, product!.Id, "Produto", null, "Criado", $"importação:{importId}") { CreatedBy = user });
                        else if (changes.Count > 0) { product!.UpdatedBy = user; foreach (var change in changes) db.ProductChangeHistories.Add(new(company, product.Id, change.Key, change.Value.Value, change.Value.Novo, $"importação:{importId}") { CreatedBy = user }); }
                        if (barcode is not null) barcodeOwners[barcode] = product!.Id;
                        ApplyInitialStock(importId, item, company, user, options, row, product!, balances, initialProducts, existingOperationKeys);
                        item.ConcluirExecucao(inserting ? StatusLinhaImportacao.Inserida : changes.Count == 0 ? StatusLinhaImportacao.SemAlteracao : StatusLinhaImportacao.Atualizada,
                            product!.Id, inserting ? "Produto inserido." : changes.Count == 0 ? "Nenhuma alteração identificada." : "Produto atualizado.", changes.Count == 0 ? null : JsonSerializer.Serialize(changes));
                    }
                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            });
            db.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            logger.LogWarning(ex, "Lote {First}-{Last} revertido na importação {ImportId}", lines[0], lines[^1], importId);
            var failureStrategy = db.Database.CreateExecutionStrategy();
            await failureStrategy.ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                var failed = await db.ImportacaoItens.Where(x => x.CompanyId == company && x.ImportacaoHistoricoId == importId && lines.Contains(x.NumeroLinha)).ToListAsync(ct);
                foreach (var item in failed)
                {
                    if (item.Status == StatusLinhaImportacao.ComErro) { item.PrepararExecucao(OperacaoExecucaoImportacao.Bloquear); item.ConcluirExecucao(StatusLinhaImportacao.Bloqueada, null, "Linha bloqueada pela validação."); }
                    else if (item.Status == StatusLinhaImportacao.Ignorada) { item.PrepararExecucao(OperacaoExecucaoImportacao.Ignorar); item.ConcluirExecucao(StatusLinhaImportacao.Ignorada, null, "Linha ignorada."); }
                    else { item.PrepararExecucao(OperacaoExecucaoImportacao.Bloquear); item.ConcluirExecucao(StatusLinhaImportacao.Falhou, null, "O lote foi revertido por conflito de persistência."); }
                }
                await db.SaveChangesAsync(ct);
            });
            db.ChangeTracker.Clear(); if (!options.PermitirImportacaoParcial) throw;
        }
    }

    private void ApplyInitialStock(Guid importId, ImportacaoItem item, Guid company, Guid? user, OpcoesValidacaoImportacao options,
        Dictionary<string, JsonElement> row, Product product, Dictionary<Guid, StockBalance> balances, HashSet<Guid> initialProducts, HashSet<string> existingOperationKeys)
    {
        var quantity = DecimalValue(row, "estoqueInicial", 0); if (quantity <= 0) return;
        if (product.ProductType != ProductType.Own || !product.ControlsStock) throw new ImportacaoExecucaoException($"Linha {item.NumeroLinha}: estoque inicial é exclusivo de produto próprio com controle de estoque.");
        if (!options.DepositoId.HasValue || !options.LocalInternoId.HasValue) throw new ImportacaoExecucaoException($"Linha {item.NumeroLinha}: depósito/local de estoque não foi selecionado.");
        var operationKey = StockOperationKey(importId, item.Id);
        if (existingOperationKeys.Contains(operationKey)) return;
        if (initialProducts.Contains(product.Id)) throw new ImportacaoExecucaoException($"Linha {item.NumeroLinha}: o produto já possui saldo inicial neste depósito.");
        if (!balances.TryGetValue(product.Id, out var balance)) { balance = new StockBalance(company, product.Id, options.DepositoId.Value) { CreatedBy = user }; db.StockBalances.Add(balance); balances[product.Id] = balance; }
        balance.Apply(StockMovementType.InitialBalance, quantity, "Importação — estoque inicial", options.LocalInternoId, $"Linha {item.NumeroLinha}", DecimalValue(row, "precoCompra", 0),
            "ImportacaoHistorico", importId.ToString(), null, operationKey, user);
        initialProducts.Add(product.Id);
        existingOperationKeys.Add(operationKey);
    }

    private static string StockOperationKey(Guid importId, Guid itemId) => $"importacao:{importId:N}:{itemId:N}:estoque-inicial";

    private async Task CompleteHistoryAsync(Guid id, Guid company, CancellationToken ct, bool fatalFailure = false)
    {
        db.ChangeTracker.Clear(); var history = await db.ImportacoesHistorico.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == company, ct); if (history is null || history.Status != StatusImportacao.Importando) return;
        var counts = await db.ImportacaoItens.AsNoTracking().Where(x => x.CompanyId == company && x.ImportacaoHistoricoId == id).GroupBy(_ => 1).Select(g => new
        { Total = g.Count(), Inserted = g.Count(x => x.Status == StatusLinhaImportacao.Inserida), Updated = g.Count(x => x.Status == StatusLinhaImportacao.Atualizada), Unchanged = g.Count(x => x.Status == StatusLinhaImportacao.SemAlteracao), Ignored = g.Count(x => x.Status == StatusLinhaImportacao.Ignorada), Blocked = g.Count(x => x.Status == StatusLinhaImportacao.Bloqueada || x.Status == StatusLinhaImportacao.ComErro), Failed = g.Count(x => x.Status == StatusLinhaImportacao.Falhou) }).FirstOrDefaultAsync(ct);
        history.FinalizarExecucao(counts?.Total ?? history.TotalLinhas, counts?.Inserted ?? 0, counts?.Updated ?? 0, counts?.Unchanged ?? 0, counts?.Ignored ?? 0, counts?.Blocked ?? 0, (counts?.Failed ?? 0) + (fatalFailure ? 1 : 0)); await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
    }

    public async Task<ResultadoExecucaoImportacao?> ObterResultadoAsync(Guid id, Guid company, CancellationToken ct = default)
    {
        var h = await db.ImportacoesHistorico.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == company, ct); if (h is null) return null;
        var items = await db.ImportacaoItens.AsNoTracking().Where(x => x.ImportacaoHistoricoId == id && x.CompanyId == company).OrderBy(x => x.NumeroLinha).ToListAsync(ct);
        var warnings = await db.ImportacaoErros.AsNoTracking().CountAsync(x => x.CompanyId == company && x.ImportacaoHistoricoId == id && x.Severidade == SeveridadeValidacao.Aviso, ct);
        return Build(h, items, h.IniciadoEm ?? h.CreatedAt, warnings);
    }

    public async Task<PaginaResultadoExecucao?> ObterResultadoPaginaAsync(Guid id, Guid company, string? filtro, string? busca, int pagina, int tamanhoPagina = 50, CancellationToken ct = default)
    {
        var history = await db.ImportacoesHistorico.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == company, ct); if (history is null) return null;
        var query = db.ImportacaoItens.AsNoTracking().Where(x => x.ImportacaoHistoricoId == id && x.CompanyId == company);
        query = filtro switch { "inseridos" => query.Where(x => x.Status == StatusLinhaImportacao.Inserida), "atualizados" => query.Where(x => x.Status == StatusLinhaImportacao.Atualizada), "semAlteracao" => query.Where(x => x.Status == StatusLinhaImportacao.SemAlteracao), "ignorados" => query.Where(x => x.Status == StatusLinhaImportacao.Ignorada), "bloqueados" => query.Where(x => x.Status == StatusLinhaImportacao.Bloqueada), "falhas" => query.Where(x => x.Status == StatusLinhaImportacao.Falhou), _ => query };
        if (!string.IsNullOrWhiteSpace(busca)) { var term = busca.Trim(); query = query.Where(x => x.NumeroLinha.ToString() == term || x.DadosOriginaisJson.Contains(term) || (x.DadosNormalizadosJson != null && x.DadosNormalizadosJson.Contains(term)) || (x.MensagemExecucao != null && x.MensagemExecucao.Contains(term))); }
        var total = await query.CountAsync(ct); var size = Math.Clamp(tamanhoPagina, 10, 200); var pages = Math.Max(1, (int)Math.Ceiling(total / (double)size)); pagina = Math.Clamp(pagina, 1, pages);
        var items = await query.OrderBy(x => x.NumeroLinha).Skip((pagina - 1) * size).Take(size).ToListAsync(ct);
        var warnings = await db.ImportacaoErros.AsNoTracking().CountAsync(x => x.CompanyId == company && x.ImportacaoHistoricoId == id && x.Severidade == SeveridadeValidacao.Aviso, ct);
        var result = Build(history, [], history.IniciadoEm ?? history.CreatedAt, warnings);
        var projected = items.Select(i => { var data = string.IsNullOrWhiteSpace(i.DadosNormalizadosJson) ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(i.DadosNormalizadosJson) ?? []; return new ResultadoExecucaoItem(i.NumeroLinha, Text(data, "codigo"), Text(data, "descricao"), i.OperacaoExecucao, i.Status, i.ProdutoId, i.MensagemExecucao); }).ToList();
        return new(result, projected, pagina, pages, total);
    }

    private static Dictionary<string, JsonElement> Parse(ImportacaoItem i) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(i.DadosNormalizadosJson!) ?? [];
    private static string? Text(Dictionary<string, JsonElement> d, string k) => d.TryGetValue(k, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : null;
    private static decimal DecimalValue(Dictionary<string, JsonElement> d, string k, decimal fallback) => d.TryGetValue(k, out var v) && v.TryGetDecimal(out var x) ? x : fallback;
    private static bool BoolValue(Dictionary<string, JsonElement> d, string k, bool fallback) => d.TryGetValue(k, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : fallback;
    private static Guid GuidValue(Dictionary<string, JsonElement> d, string k) => Guid.TryParse(Text(d, k), out var x) ? x : Guid.Empty;
    private static Guid? OptionalGuid(Dictionary<string, JsonElement> d, string k, Guid? fallback, bool ignoreEmpty) => d.ContainsKey(k) ? Guid.TryParse(Text(d, k), out var x) ? x : ignoreEmpty ? fallback : null : fallback;
    private static ProductType ProductTypeValue(Dictionary<string, JsonElement> d, ProductType fallback = ProductType.Own) => !d.ContainsKey("tipoProduto") ? fallback : Text(d, "tipoProduto")?.Contains("Terceiro", StringComparison.OrdinalIgnoreCase) == true ? ProductType.ThirdParty : ProductType.Own;
    private static void Apply(Product p, Dictionary<string, JsonElement> d, OpcoesValidacaoImportacao options, bool ignoreEmpty)
    {
        string? Get(string k, string? old) => d.ContainsKey(k) ? Text(d, k) ?? (ignoreEmpty ? old : null) : old;
        var unit = GuidValue(d, "unidadeId"); if (unit == Guid.Empty) unit = p.UnitOfMeasureId; var type = ProductTypeValue(d, p.ProductType); var controls = type == ProductType.Own && BoolValue(d, "controlaEstoque", p.ControlsStock);
        p.Update(p.InternalCode, p.Sku, Get("codigoBarras", p.Barcode), p.Reference, Get("descricao", p.Name)!, p.ShortDescription, Get("descricaoComplementar", p.Description), type, controls,
            BoolValue(d, "status", p.IsActive), OptionalGuid(d, "categoriaId", p.CategoryId, ignoreEmpty), OptionalGuid(d, "subcategoriaId", p.SubcategoryId, ignoreEmpty), OptionalGuid(d, "marcaId", p.BrandId, ignoreEmpty), unit,
            OptionalGuid(d, "grupoId", p.ProductGroupId, ignoreEmpty), OptionalGuid(d, "fornecedorId", p.MainSupplierId, ignoreEmpty), OptionalGuid(d, "parceiroId", p.PartnerId, ignoreEmpty),
            controls && DecimalValue(d, "estoqueInicial", 0) > 0 ? options.DepositoId : p.DefaultWarehouseId, controls && DecimalValue(d, "estoqueInicial", 0) > 0 ? options.LocalInternoId : p.DefaultWarehouseLocationId,
            Get("ncm", p.Ncm), p.Cest, DecimalValue(d, "precoCompra", p.CostPrice), DecimalValue(d, "precoVenda", p.SalePrice), p.CommissionType, p.CommissionValue, p.PriceValidUntil, p.MinimumStock, Get("observacoes", p.Notes));
    }
    private static Dictionary<string, string?> Snapshot(Product p) => new() { ["Descrição"] = p.Name, ["Descrição complementar"] = p.Description, ["Código de barras"] = p.Barcode, ["Preço de custo"] = p.CostPrice.ToString(), ["Preço de venda"] = p.SalePrice.ToString(), ["Unidade"] = p.UnitOfMeasureId.ToString(), ["Tipo"] = p.ProductType.ToString(), ["Categoria"] = p.CategoryId?.ToString(), ["Subcategoria"] = p.SubcategoryId?.ToString(), ["Marca"] = p.BrandId?.ToString(), ["Grupo"] = p.ProductGroupId?.ToString(), ["Fornecedor"] = p.MainSupplierId?.ToString(), ["Parceiro"] = p.PartnerId?.ToString(), ["NCM"] = p.Ncm, ["Status"] = p.IsActive.ToString(), ["Controla estoque"] = p.ControlsStock.ToString(), ["Observações"] = p.Notes };
    private static ResultadoExecucaoImportacao Build(ImportacaoHistorico h, IReadOnlyList<ImportacaoItem> items, DateTimeOffset start, int warnings) => new(h.Id, h.TotalLinhas, h.ProdutosInseridos, h.ProdutosAtualizados, h.SemAlteracao, h.LinhasIgnoradas, h.ItensBloqueados, h.FalhasExecucao, warnings, start, h.FinalizadoEm ?? DateTimeOffset.UtcNow, h.Status, [], items.Select(i => { var data = string.IsNullOrWhiteSpace(i.DadosNormalizadosJson) ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(i.DadosNormalizadosJson) ?? []; return new ResultadoExecucaoItem(i.NumeroLinha, Text(data, "codigo"), Text(data, "descricao"), i.OperacaoExecucao, i.Status, i.ProdutoId, i.MensagemExecucao); }).ToList());
}
