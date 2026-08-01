using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class ContextoValidacaoImportacaoService(ApplicationDbContext db) : IContextoValidacaoImportacaoService
{
    public async Task<ContextoValidacaoImportacao> PrepararAsync(Guid importacaoId, Guid empresaId, Guid? usuarioId,
        IReadOnlyList<LinhaPlanilhaImportada> linhas, MapeamentoColunasImportacao mapeamento,
        OpcoesValidacaoImportacao opcoes, CancellationToken cancellationToken = default)
    {
        var codigos = linhas.Select(x => Valor(x, mapeamento, "codigo"))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(ValidadorDadosImportacaoService.NormalizarCodigo).Distinct().ToList();
        var barras = linhas.Select(x => Valor(x, mapeamento, "codigoBarras")?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var produtos = await db.Products.AsNoTracking()
            .Where(x => x.CompanyId == empresaId && (codigos.Contains(x.InternalCode) || x.Barcode != null && barras.Contains(x.Barcode)))
            .Select(x => new ProdutoExistenteImportacao(
                x.Id, x.InternalCode, x.Name, x.Barcode, x.CostPrice, x.SalePrice,
                x.UnitOfMeasureId, x.IsActive, x.ProductType, x.PartnerId, x.ControlsStock,
                x.Description, x.CategoryId, x.SubcategoryId, x.BrandId, x.ProductGroupId,
                x.MainSupplierId, x.DefaultWarehouseId, x.DefaultWarehouseLocationId,
                x.Ncm, x.Notes, x.MinimumStock))
            .ToListAsync(cancellationToken);
        var codigosExistentes = produtos.Select(x => ValidadorDadosImportacaoService.NormalizarCodigo(x.Codigo))
            .ToHashSet(StringComparer.Ordinal);
        var normalizacao = NormalizadorDadosImportacao.Normalizar(linhas, mapeamento, linha =>
            !opcoes.IgnorarVaziosAtualizacao ||
            !codigosExistentes.Contains(ValidadorDadosImportacaoService.NormalizarCodigo(Valor(linha, mapeamento, "codigo"))));
        linhas = normalizacao.Linhas;
        var produtosComSaldoInicialNoDeposito = new HashSet<Guid>();
        if (opcoes.DepositoId.HasValue)
        {
            var codigosComEstoqueInicial = linhas
                .Where(linha => ValidadorDadosImportacaoService.TryDecimal(
                    Valor(linha, mapeamento, "estoqueInicial") ?? string.Empty, out var quantidade) && quantidade > 0)
                .Select(linha => ValidadorDadosImportacaoService.NormalizarCodigo(Valor(linha, mapeamento, "codigo")))
                .ToHashSet(StringComparer.Ordinal);
            var produtosCandidatos = produtos
                .Where(produto => codigosComEstoqueInicial.Contains(ValidadorDadosImportacaoService.NormalizarCodigo(produto.Codigo)))
                .Select(produto => produto.Id)
                .ToList();

            if (produtosCandidatos.Count > 0)
            {
                produtosComSaldoInicialNoDeposito = (await db.StockMovements.AsNoTracking()
                    .Where(movimento => movimento.CompanyId == empresaId &&
                        movimento.WarehouseId == opcoes.DepositoId &&
                        movimento.Type == StockMovementType.InitialBalance &&
                        produtosCandidatos.Contains(movimento.ProductId))
                    .Select(movimento => movimento.ProductId)
                    .Distinct()
                    .ToListAsync(cancellationToken))
                    .ToHashSet();
            }
        }

        var unidades = await db.UnitsOfMeasure.AsNoTracking().Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new ReferenciaImportacao(x.Id, x.Code ?? x.Abbreviation, x.Name, null, null)).ToListAsync(cancellationToken);
        var marcas = await db.Brands.AsNoTracking().Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new ReferenciaImportacao(x.Id, x.Code, x.Name, null, null)).ToListAsync(cancellationToken);
        var categorias = await db.Categories.AsNoTracking().Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new ReferenciaImportacao(x.Id, x.Code, x.Name, null, null)).ToListAsync(cancellationToken);
        var subcategorias = await db.Subcategories.AsNoTracking().Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new ReferenciaImportacao(x.Id, x.Code, x.Name, null, x.CategoryId)).ToListAsync(cancellationToken);
        var grupos = await db.ProductGroups.AsNoTracking().Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new ReferenciaImportacao(x.Id, x.Code, x.Name, null, null)).ToListAsync(cancellationToken);
        var fornecedores = await db.Suppliers.AsNoTracking().Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new ReferenciaImportacao(x.Id, null, x.LegalName, x.Document, null)).ToListAsync(cancellationToken);
        var parceiros = await db.CommercialPartners.AsNoTracking().Where(x => x.CompanyId == empresaId && x.IsActive)
            .Select(x => new ReferenciaImportacao(x.Id, null, x.Name, x.Document, null)).ToListAsync(cancellationToken);

        var depositoValido = !opcoes.DepositoId.HasValue || await db.Warehouses.AsNoTracking()
            .AnyAsync(x => x.CompanyId == empresaId && x.Id == opcoes.DepositoId && x.IsActive, cancellationToken);
        var localValido = !opcoes.LocalInternoId.HasValue || await db.InternalLocations.AsNoTracking()
            .AnyAsync(x => x.CompanyId == empresaId && x.Id == opcoes.LocalInternoId && x.WarehouseId == opcoes.DepositoId && x.IsActive, cancellationToken);

        var referencias = new ReferenciasProdutoImportacao(unidades, marcas, categorias, subcategorias, grupos,
            fornecedores, parceiros, depositoValido, localValido);
        return new(importacaoId, empresaId, usuarioId, linhas, mapeamento, opcoes, produtos, referencias,
            normalizacao.QuantidadeUnidadesPreenchidasAutomaticamente, produtosComSaldoInicialNoDeposito);
    }

    private static string? Valor(LinhaPlanilhaImportada linha, MapeamentoColunasImportacao mapeamento, string campo) =>
        mapeamento.Colunas.TryGetValue(campo, out var coluna) && linha.Valores.TryGetValue(coluna, out var valor) ? valor : null;
}
