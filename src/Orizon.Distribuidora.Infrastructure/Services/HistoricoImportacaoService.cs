using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Domain.Enums;
using System.Text.Json;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class HistoricoImportacaoService : IHistoricoImportacaoService
{
    private readonly ApplicationDbContext dbContext;

    public HistoricoImportacaoService(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ImportacaoHistorico> RegistrarAsync(
        Guid companyId,
        ArquivoImportacaoExcel arquivo,
        Guid? usuarioId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arquivo);

        var historico = new ImportacaoHistorico(
            companyId,
            arquivo.NomeArquivo,
            arquivo.TipoArquivo,
            arquivo.TamanhoBytes);

        historico.CreatedBy = usuarioId;
        dbContext.ImportacoesHistorico.Add(historico);
        await dbContext.SaveChangesAsync(cancellationToken);

        return historico;
    }

    public async Task<IReadOnlyList<ImportacaoHistorico>> ListarAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ImportacoesHistorico
            .AsNoTracking()
            .Where(importacao => importacao.CompanyId == companyId)
            .OrderByDescending(importacao => importacao.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ImportacaoHistorico?> ObterAsync(
        Guid companyId,
        Guid importacaoId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ImportacoesHistorico
            .AsNoTracking()
            .FirstOrDefaultAsync(
                importacao => importacao.CompanyId == companyId && importacao.Id == importacaoId,
                cancellationToken);
    }

    public async Task SalvarValidacaoAsync(Guid companyId, Guid importacaoId, Guid? usuarioId, ResultadoValidacaoImportacao resultado, OpcoesValidacaoImportacao opcoes, CancellationToken cancellationToken = default)
    {
        var historico=await dbContext.ImportacoesHistorico.FirstOrDefaultAsync(x=>x.CompanyId==companyId&&x.Id==importacaoId,cancellationToken)??throw new KeyNotFoundException();
        await dbContext.ImportacaoErros.Where(x=>x.CompanyId==companyId&&x.ImportacaoHistoricoId==importacaoId).ExecuteUpdateAsync(x=>x.SetProperty(e=>e.IsDeleted,true).SetProperty(e=>e.DeletedAt,DateTimeOffset.UtcNow),cancellationToken);
        await dbContext.ImportacaoItens.Where(x=>x.CompanyId==companyId&&x.ImportacaoHistoricoId==importacaoId).ExecuteUpdateAsync(x=>x.SetProperty(e=>e.IsDeleted,true).SetProperty(e=>e.DeletedAt,DateTimeOffset.UtcNow),cancellationToken);
        foreach(var linha in resultado.Linhas){var item=new ImportacaoItem(companyId,importacaoId,linha.NumeroLinha,JsonSerializer.Serialize(linha.DadosOriginais));if(linha.Status==StatusValidacaoLinha.Ignorada)item.Ignorar();else if(linha.Erros.Count>0)item.MarcarComErro();else item.MarcarComoValida(JsonSerializer.Serialize(linha.ValoresConvertidos));dbContext.ImportacaoItens.Add(item);foreach(var p in linha.Erros.Concat(linha.Avisos)){var e=new ImportacaoErro(companyId,importacaoId,p.Mensagem,item.Id,linha.NumeroLinha,p.Campo,p.ValorOriginal);e.DefinirClassificacao(p.Codigo,p.Severidade);dbContext.ImportacaoErros.Add(e);}}
        historico.RegistrarValidacao(resultado.TotalLinhas,resultado.QuantidadeValida,resultado.QuantidadeComErro,resultado.QuantidadeComAviso,resultado.QuantidadeNovos,resultado.QuantidadeExistentes,resultado.QuantidadeAtualizaveis,resultado.QuantidadeDuplicidades,resultado.QuantidadeIgnoradas,resultado.PodeImportar,usuarioId,JsonSerializer.Serialize(opcoes));await dbContext.SaveChangesAsync(cancellationToken);
    }
}
