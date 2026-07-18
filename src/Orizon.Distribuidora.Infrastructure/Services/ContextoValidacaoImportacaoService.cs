using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class ContextoValidacaoImportacaoService(ApplicationDbContext db) : IContextoValidacaoImportacaoService
{
    public async Task<ContextoValidacaoImportacao> PrepararAsync(Guid importacaoId, Guid empresaId, Guid? usuarioId, IReadOnlyList<LinhaPlanilhaImportada> linhas, MapeamentoColunasImportacao mapeamento, OpcoesValidacaoImportacao opcoes, CancellationToken cancellationToken = default)
    {
        var codigos = linhas.Select(x => Valor(x,mapeamento,"codigo")).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(ValidadorDadosImportacaoService.NormalizarCodigo).Distinct().ToList();
        var barras = linhas.Select(x => Valor(x,mapeamento,"codigoBarras")?.Trim()).Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var produtos = await db.Products.AsNoTracking().Where(x=>x.CompanyId==empresaId && (codigos.Contains(x.InternalCode) || x.Barcode!=null && barras.Contains(x.Barcode))).Select(x=>new ProdutoExistenteImportacao(x.Id,x.InternalCode,x.Name,x.Barcode,x.CostPrice,x.SalePrice,x.UnitOfMeasureId,x.IsActive)).ToListAsync(cancellationToken);
        var unitsRaw = await db.UnitsOfMeasure.AsNoTracking().Where(x=>x.CompanyId==empresaId && x.IsActive).Select(x=>new{x.Id,x.Name,x.Code,x.Abbreviation}).ToListAsync(cancellationToken);
        var units = new Dictionary<string,Guid>(); foreach(var x in unitsRaw) foreach(var key in new[]{x.Name,x.Code,x.Abbreviation}.Where(v=>!string.IsNullOrWhiteSpace(v)).Select(v=>ValidadorDadosImportacaoService.NormalizarTexto(v!))) if(!units.ContainsKey(key)) units[key]=x.Id;
        return new(importacaoId,empresaId,usuarioId,linhas,mapeamento,opcoes,produtos,units);
    }
    private static string? Valor(LinhaPlanilhaImportada l,MapeamentoColunasImportacao m,string campo)=>m.Colunas.TryGetValue(campo,out var c)&&l.Valores.TryGetValue(c,out var v)?v:null;
}
