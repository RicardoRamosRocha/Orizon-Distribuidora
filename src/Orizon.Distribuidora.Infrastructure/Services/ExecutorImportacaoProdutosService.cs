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

public sealed class ExecutorImportacaoProdutosService(ApplicationDbContext db,ILogger<ExecutorImportacaoProdutosService> logger) : IExecutorImportacaoProdutosService
{
    private const int BatchSize=200;
    public async Task<ResultadoExecucaoImportacao> ExecutarAsync(Guid importacaoId,Guid empresaId,Guid? usuarioId,CancellationToken ct=default)
    {
        var inicio=DateTimeOffset.UtcNow;
        await using(var lockTx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct))
        {
            var h=await db.ImportacoesHistorico.FirstOrDefaultAsync(x=>x.Id==importacaoId&&x.CompanyId==empresaId,ct)??throw new ImportacaoExecucaoException("Importação não encontrada para a empresa atual.");
            if(h.Status is StatusImportacao.Concluida or StatusImportacao.ConcluidaParcialmente)throw new ImportacaoExecucaoException("Esta importação já foi executada.");
            if(h.Status==StatusImportacao.Importando)throw new ImportacaoExecucaoException("Esta importação já está sendo executada.");
            if(string.IsNullOrWhiteSpace(h.OpcoesValidacaoJson))throw new ImportacaoExecucaoException("As opções validadas da importação não estão disponíveis.");
            h.IniciarExecucao(usuarioId);await db.SaveChangesAsync(ct);await lockTx.CommitAsync(ct);
        }
        db.ChangeTracker.Clear();
        try
        {
            var historico=await db.ImportacoesHistorico.FirstAsync(x=>x.Id==importacaoId&&x.CompanyId==empresaId,ct);
            var options=JsonSerializer.Deserialize<OpcoesValidacaoImportacao>(historico.OpcoesValidacaoJson!)??new();
            var items=await db.ImportacaoItens.Where(x=>x.CompanyId==empresaId&&x.ImportacaoHistoricoId==importacaoId).OrderBy(x=>x.NumeroLinha).ToListAsync(ct);
            if(!options.PermitirImportacaoParcial&&items.Any(x=>x.Status==StatusLinhaImportacao.ComErro)){historico.FinalizarExecucao(items.Count,0,0,0,items.Count(x=>x.Status==StatusLinhaImportacao.Ignorada),items.Count(x=>x.Status==StatusLinhaImportacao.ComErro),0);await db.SaveChangesAsync(ct);return Build(historico,items,inicio,0);}
            var normalized=items.Where(x=>x.Status==StatusLinhaImportacao.Valida&&!string.IsNullOrWhiteSpace(x.DadosNormalizadosJson)).ToDictionary(x=>x,Parse);
            var codes=normalized.Values.Select(x=>Text(x,"codigo")).Where(x=>x is not null).ToList();
            var products=await db.Products.Where(x=>x.CompanyId==empresaId&&codes.Contains(x.InternalCode)).ToDictionaryAsync(x=>x.InternalCode,StringComparer.Ordinal,ct);
            var allBarcodes=await db.Products.AsNoTracking().Where(x=>x.CompanyId==empresaId&&x.Barcode!=null).Select(x=>new{x.Id,x.Barcode}).ToDictionaryAsync(x=>x.Barcode!,x=>x.Id,StringComparer.Ordinal,ct);
            var validUnits=(await db.UnitsOfMeasure.AsNoTracking().Where(x=>x.CompanyId==empresaId&&x.IsActive).Select(x=>x.Id).ToListAsync(ct)).ToHashSet();
            var warnings=await db.ImportacaoErros.CountAsync(x=>x.CompanyId==empresaId&&x.ImportacaoHistoricoId==importacaoId&&x.Severidade==SeveridadeValidacao.Aviso,ct);
            var inserted=0;var updated=0;var unchanged=0;var ignored=0;var blocked=0;var failures=0;var pending=0;
            await using var allTx=!options.PermitirImportacaoParcial?await db.Database.BeginTransactionAsync(ct):null;
            foreach(var item in items)
            {
                ct.ThrowIfCancellationRequested();
                if(item.Status==StatusLinhaImportacao.ComErro){item.PrepararExecucao(OperacaoExecucaoImportacao.Bloquear);item.ConcluirExecucao(StatusLinhaImportacao.Bloqueada,null,"Linha bloqueada pela validação.");blocked++;continue;}
                if(item.Status==StatusLinhaImportacao.Ignorada){item.PrepararExecucao(OperacaoExecucaoImportacao.Ignorar);item.ConcluirExecucao(StatusLinhaImportacao.Ignorada,null,"Linha ignorada.");ignored++;continue;}
                if(!normalized.TryGetValue(item,out var data)){item.PrepararExecucao(OperacaoExecucaoImportacao.Bloquear);item.ConcluirExecucao(StatusLinhaImportacao.Bloqueada,null,"Dados validados indisponíveis.");blocked++;continue;}
                try
                {
                    var code=Text(data,"codigo")!;var name=Text(data,"descricao")!;var barcode=Text(data,"codigoBarras");
                    products.TryGetValue(code,out var product);
                    if(product is null)
                    {
                        item.PrepararExecucao(OperacaoExecucaoImportacao.Inserir);
                        if(!options.InserirNovos){item.ConcluirExecucao(StatusLinhaImportacao.Ignorada,null,"Inclusão de novos desativada.");ignored++;continue;}
                        if(allBarcodes.TryGetValue(barcode??"",out _))throw new ImportacaoExecucaoException("IMP_EXEC_CODIGO_BARRAS_DUPLICADO: código de barras já utilizado.");
                        var unit=GuidValue(data,"unidadeId");if(unit==Guid.Empty||!validUnits.Contains(unit))throw new ImportacaoExecucaoException("IMP_EXEC_REFERENCIA_INVALIDA: unidade não está mais disponível.");
                        var type=ProductTypeValue(data);if(type==ProductType.ThirdParty)throw new ImportacaoExecucaoException("IMP_EXEC_REFERENCIA_INVALIDA: produto de terceiro exige parceiro comercial.");
                        product=new Product(empresaId,code,name,unit,type);Apply(product,data,true);product.CreatedBy=usuarioId;db.Products.Add(product);db.ProductChangeHistories.Add(new(empresaId,product.Id,"Produto",null,"Criado",$"importação:{importacaoId}" ){CreatedBy=usuarioId});products[code]=product;if(barcode is not null)allBarcodes[barcode]=product.Id;item.ConcluirExecucao(StatusLinhaImportacao.Inserida,product.Id,"Produto inserido.");inserted++;
                    }
                    else
                    {
                        item.PrepararExecucao(OperacaoExecucaoImportacao.Atualizar);
                        if(!options.AtualizarExistentes){item.ConcluirExecucao(StatusLinhaImportacao.Ignorada,product.Id,"Atualização desativada.");ignored++;continue;}
                        if(barcode is not null&&allBarcodes.TryGetValue(barcode,out var owner)&&owner!=product.Id)throw new ImportacaoExecucaoException("IMP_EXEC_CODIGO_BARRAS_DUPLICADO: código de barras já utilizado.");
                        var before=Snapshot(product);Apply(product,data,false,options.IgnorarVaziosAtualizacao);var after=Snapshot(product);var changes=before.Where(x=>after[x.Key]!=x.Value).ToDictionary(x=>x.Key,x=>new{x.Value,Novo=after[x.Key]});
                        if(changes.Count==0){db.Entry(product).State=EntityState.Unchanged;item.ConcluirExecucao(StatusLinhaImportacao.SemAlteracao,product.Id,"Nenhuma alteração identificada.");unchanged++;}
                        else{product.UpdatedBy=usuarioId;foreach(var c in changes)db.ProductChangeHistories.Add(new(empresaId,product.Id,c.Key,c.Value.Value,c.Value.Novo,$"importação:{importacaoId}"){CreatedBy=usuarioId});item.ConcluirExecucao(StatusLinhaImportacao.Atualizada,product.Id,"Produto atualizado.",JsonSerializer.Serialize(changes));updated++;}
                    }
                }
                catch(Exception ex) when(ex is ArgumentException or InvalidOperationException)
                {logger.LogWarning(ex,"Falha esperada na linha {Line} da importação {ImportId}",item.NumeroLinha,importacaoId);item.ConcluirExecucao(StatusLinhaImportacao.Falhou,null,ex.Message);failures++;if(!options.PermitirImportacaoParcial)throw;}
                pending++;if(options.PermitirImportacaoParcial&&pending>=BatchSize){await db.SaveChangesAsync(ct);pending=0;}
            }
            historico.FinalizarExecucao(items.Count,inserted,updated,unchanged,ignored,blocked,failures);await db.SaveChangesAsync(ct);if(allTx is not null)await allTx.CommitAsync(ct);return Build(historico,items,inicio,warnings);
        }
        catch(Exception ex)
        {logger.LogError(ex,"Falha geral na importação {ImportId}",importacaoId);db.ChangeTracker.Clear();var h=await db.ImportacoesHistorico.FirstOrDefaultAsync(x=>x.Id==importacaoId&&x.CompanyId==empresaId,ct);if(h is not null&&h.Status==StatusImportacao.Importando){h.FinalizarExecucao(h.TotalLinhas,0,0,0,0,0,1);await db.SaveChangesAsync(ct);}throw new ImportacaoExecucaoException("Não foi possível concluir a importação. Nenhum detalhe técnico foi exposto.");}
    }
    public async Task<ResultadoExecucaoImportacao?> ObterResultadoAsync(Guid id,Guid company,CancellationToken ct=default){var h=await db.ImportacoesHistorico.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==company,ct);if(h is null)return null;var items=await db.ImportacaoItens.AsNoTracking().Where(x=>x.ImportacaoHistoricoId==id&&x.CompanyId==company).OrderBy(x=>x.NumeroLinha).ToListAsync(ct);return Build(h,items,h.IniciadoEm??h.CreatedAt,0);}
    private static Dictionary<string,JsonElement> Parse(ImportacaoItem i)=>JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(i.DadosNormalizadosJson!)??[];
    private static string? Text(Dictionary<string,JsonElement>d,string k)=>d.TryGetValue(k,out var v)&&v.ValueKind!=JsonValueKind.Null?v.ToString():null;
    private static decimal DecimalValue(Dictionary<string,JsonElement>d,string k,decimal fallback)=>d.TryGetValue(k,out var v)&&v.TryGetDecimal(out var x)?x:fallback;
    private static Guid GuidValue(Dictionary<string,JsonElement>d,string k)=>Guid.TryParse(Text(d,k),out var x)?x:Guid.Empty;
    private static ProductType ProductTypeValue(Dictionary<string,JsonElement>d,ProductType fallback=ProductType.Own)=>!d.ContainsKey("tipoProduto")?fallback:Text(d,"tipoProduto")?.Contains("Terceiro",StringComparison.OrdinalIgnoreCase)==true?ProductType.ThirdParty:ProductType.Own;
    private static void Apply(Product p,Dictionary<string,JsonElement>d,bool creating,bool ignoreEmpty=true){string? Get(string k,string? old)=>d.ContainsKey(k)?Text(d,k)??(ignoreEmpty?old:null):old;var unit=GuidValue(d,"unidadeId");if(unit==Guid.Empty)unit=p.UnitOfMeasureId;var type=ProductTypeValue(d,p.ProductType);p.Update(p.InternalCode,p.Sku,Get("codigoBarras",p.Barcode),p.Reference,Get("descricao",p.Name)!,p.ShortDescription,Get("descricaoComplementar",p.Description),type,type!=ProductType.ThirdParty,p.IsActive,p.CategoryId,p.SubcategoryId,p.BrandId,unit,p.ProductGroupId,p.MainSupplierId,p.PartnerId,p.DefaultWarehouseId,p.DefaultWarehouseLocationId,Get("ncm",p.Ncm),p.Cest,DecimalValue(d,"precoCompra",p.CostPrice),DecimalValue(d,"precoVenda",p.SalePrice),p.CommissionType,p.CommissionValue,p.PriceValidUntil,p.MinimumStock,Get("observacoes",p.Notes));}
    private static Dictionary<string,string?> Snapshot(Product p)=>new(){{"Descrição",p.Name},{"Código de barras",p.Barcode},{"Preço de custo",p.CostPrice.ToString()},{"Preço de venda",p.SalePrice.ToString()},{"Unidade",p.UnitOfMeasureId.ToString()},{"Tipo",p.ProductType.ToString()}};
    private static ResultadoExecucaoImportacao Build(ImportacaoHistorico h,IReadOnlyList<ImportacaoItem> items,DateTimeOffset start,int warnings)=>new(h.Id,items.Count,h.ProdutosInseridos,h.ProdutosAtualizados,h.SemAlteracao,h.LinhasIgnoradas,h.ItensBloqueados,h.FalhasExecucao,warnings,start,h.FinalizadoEm??DateTimeOffset.UtcNow,h.Status,[],items.Select(i=>{var data=string.IsNullOrWhiteSpace(i.DadosNormalizadosJson)?[]:JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(i.DadosNormalizadosJson)??[];return new ResultadoExecucaoItem(i.NumeroLinha,Text(data,"codigo"),Text(data,"descricao"),i.OperacaoExecucao,i.Status,i.ProdutoId,i.MensagemExecucao);}).ToList());
}
