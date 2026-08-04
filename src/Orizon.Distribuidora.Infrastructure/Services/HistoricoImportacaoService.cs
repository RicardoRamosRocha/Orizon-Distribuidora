using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Domain.Enums;
using System.Text.Json;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed partial class HistoricoImportacaoService : IHistoricoImportacaoService
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

    public async Task CancelarAsync(Guid companyId, Guid importacaoId, Guid? usuarioId, CancellationToken cancellationToken = default)
    {
        var historico = await dbContext.ImportacoesHistorico
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == importacaoId, cancellationToken)
            ?? throw new KeyNotFoundException();
        historico.Cancelar("Cancelada pelo usuÃ¡rio antes da validaÃ§Ã£o.");
        historico.UpdatedBy = usuarioId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SalvarValidacaoAsync(Guid companyId, Guid importacaoId, Guid? usuarioId, ResultadoValidacaoImportacao resultado, OpcoesValidacaoImportacao opcoes, CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var deletedAt = DateTimeOffset.UtcNow;
                await dbContext.ImportacaoErros.Where(x => x.CompanyId == companyId && x.ImportacaoHistoricoId == importacaoId)
                    .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsDeleted, true).SetProperty(e => e.DeletedAt, deletedAt).SetProperty(e => e.DeletedBy, usuarioId), cancellationToken);
                await dbContext.ImportacaoItens.Where(x => x.CompanyId == companyId && x.ImportacaoHistoricoId == importacaoId)
                    .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsDeleted, true).SetProperty(e => e.DeletedAt, deletedAt).SetProperty(e => e.DeletedBy, usuarioId), cancellationToken);
                foreach (var lote in resultado.Linhas.Chunk(500))
                {
                    foreach (var linha in lote)
                    {
                        var item = new ImportacaoItem(companyId, importacaoId, linha.NumeroLinha, JsonSerializer.Serialize(linha.DadosOriginais)) { CreatedBy = usuarioId };
                        var normalizados = new Dictionary<string, object?>(linha.ValoresConvertidos) { ["__operacao"] = linha.Operacao.ToString(), ["__produtoExistenteId"] = linha.ProdutoExistente?.Id };
                        if (linha.Operacao == TipoOperacaoImportacao.Ignorar)
                            item.Ignorar(JsonSerializer.Serialize(normalizados), linha.ProdutoExistente?.Id);
                        else if (linha.Erros.Count > 0) item.MarcarComErro();
                        else item.MarcarComoValida(JsonSerializer.Serialize(normalizados));
                        dbContext.ImportacaoItens.Add(item);
                        foreach (var p in linha.Erros.Concat(linha.Avisos))
                        {
                            var e = new ImportacaoErro(companyId, importacaoId, p.Mensagem, item.Id, linha.NumeroLinha, p.Campo, p.ValorOriginal) { CreatedBy = usuarioId };
                            e.DefinirClassificacao(p.Codigo, p.Severidade); dbContext.ImportacaoErros.Add(e);
                        }
                    }
                    await dbContext.SaveChangesAsync(cancellationToken);
                    dbContext.ChangeTracker.Clear();
                }
                var historico = await dbContext.ImportacoesHistorico.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == importacaoId, cancellationToken) ?? throw new KeyNotFoundException();
                historico.RegistrarValidacao(resultado.TotalLinhas, resultado.QuantidadeValida, resultado.QuantidadeComErro, resultado.QuantidadeComAviso, resultado.QuantidadeNovos, resultado.QuantidadeExistentes, resultado.QuantidadeAtualizaveis, resultado.QuantidadeDuplicidades, resultado.QuantidadeIgnoradas, resultado.PodeImportar, usuarioId, JsonSerializer.Serialize(opcoes));
                historico.UpdatedBy = usuarioId;
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task AssociarModeloAsync(Guid companyId, Guid importacaoId, Guid modeloId, Guid? usuarioId, CancellationToken cancellationToken = default)
    {
        var modelo = await dbContext.ModelosImportacao.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == modeloId && x.Ativo && (x.UsuarioId == null || x.UsuarioId == usuarioId), cancellationToken)
            ?? throw new UnauthorizedAccessException("Modelo de importação indisponível para o usuário atual.");
        var historico = await dbContext.ImportacoesHistorico.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == importacaoId, cancellationToken) ?? throw new KeyNotFoundException();
        historico.AssociarModelo(modelo.Id); historico.UpdatedBy = usuarioId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginaValidacaoImportacao?> ObterValidacaoAsync(Guid companyId, Guid importacaoId, string? filtro, string? busca, int pagina, int tamanhoPagina = 50, CancellationToken cancellationToken = default)
    {
        var historico = await dbContext.ImportacoesHistorico.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == importacaoId, cancellationToken);
        if (historico is null || string.IsNullOrWhiteSpace(historico.OpcoesValidacaoJson)) return null;
        var opcoes = JsonSerializer.Deserialize<OpcoesValidacaoImportacao>(historico.OpcoesValidacaoJson);
        var query = dbContext.ImportacaoItens.AsNoTracking().Where(x => x.CompanyId == companyId && x.ImportacaoHistoricoId == importacaoId);
        query = filtro switch
        {
            "validas" => query.Where(x => x.Status == StatusLinhaImportacao.Valida),
            "novas" => query.Where(x => x.DadosNormalizadosJson != null && x.DadosNormalizadosJson.Contains("Inserir")),
            "atualizacoes" => query.Where(x => x.DadosNormalizadosJson != null && x.DadosNormalizadosJson.Contains("Atualizar")),
            "avisos" => query.Where(x => dbContext.ImportacaoErros.Any(e => e.ImportacaoItemId == x.Id && e.Severidade == SeveridadeValidacao.Aviso)),
            "erros" => query.Where(x => x.Status == StatusLinhaImportacao.ComErro),
            "duplicadas" => query.Where(x => dbContext.ImportacaoErros.Any(e => e.ImportacaoItemId == x.Id && e.Codigo.StartsWith("IMP_DUPLICIDADE"))),
            "ignoradas" => query.Where(x => x.Status == StatusLinhaImportacao.Ignorada),
            _ => query
        };
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var term = busca.Trim();
            query = query.Where(x => x.NumeroLinha.ToString() == term || x.DadosOriginaisJson.Contains(term) ||
                (x.DadosNormalizadosJson != null && x.DadosNormalizadosJson.Contains(term)) ||
                dbContext.ImportacaoErros.Any(e => e.ImportacaoItemId == x.Id && e.Mensagem.Contains(term)));
        }
        var total = await query.CountAsync(cancellationToken); var size = Math.Clamp(tamanhoPagina, 10, 200);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)size)); pagina = Math.Clamp(pagina, 1, pages);
        var items = await query.OrderBy(x => x.NumeroLinha).Skip((pagina - 1) * size).Take(size).ToListAsync(cancellationToken);
        var ids = items.Select(x => x.Id).ToList();
        var issues = await dbContext.ImportacaoErros.AsNoTracking().Where(x => x.CompanyId == companyId && ids.Contains(x.ImportacaoItemId!.Value)).ToListAsync(cancellationToken);
        var allIssues = (await dbContext.ImportacaoErros.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.ImportacaoHistoricoId == importacaoId)
                .OrderBy(x => x.Severidade)
                .ThenBy(x => x.NumeroLinha)
                .ToListAsync(cancellationToken))
            .Select(ToValidationIssue)
            .ToList();
        var linhas = items.Select(item =>
        {
            var original = JsonSerializer.Deserialize<Dictionary<string, string?>>(item.DadosOriginaisJson) ?? [];
            var normalized = string.IsNullOrWhiteSpace(item.DadosNormalizadosJson) ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.DadosNormalizadosJson) ?? [];
            var lineIssues = issues.Where(x => x.ImportacaoItemId == item.Id).ToList();
            var erros = lineIssues.Where(x => x.Severidade == SeveridadeValidacao.Erro).Select(ToValidationIssue).ToList();
            var avisos = lineIssues.Where(x => x.Severidade == SeveridadeValidacao.Aviso).Select(ToValidationIssue).ToList();
            var operation = normalized.TryGetValue("__operacao", out var operationValue) && Enum.TryParse<TipoOperacaoImportacao>(operationValue.ToString(), out var parsed) ? parsed : item.Status == StatusLinhaImportacao.Ignorada ? TipoOperacaoImportacao.Ignorar : erros.Count > 0 ? TipoOperacaoImportacao.Bloquear : TipoOperacaoImportacao.Inserir;
            var status = erros.Any(x => x.Codigo.StartsWith("IMP_DUPLICIDADE", StringComparison.Ordinal)) ? StatusValidacaoLinha.Duplicada : erros.Count > 0 ? StatusValidacaoLinha.Invalida : item.Status == StatusLinhaImportacao.Ignorada ? StatusValidacaoLinha.Ignorada : avisos.Count > 0 ? StatusValidacaoLinha.ComAviso : StatusValidacaoLinha.Valida;
            var converted = normalized.Where(x => !x.Key.StartsWith("__", StringComparison.Ordinal)).ToDictionary(x => x.Key, x => (object?)x.Value);
            var code = normalized.TryGetValue("codigo", out var c) ? c.ToString() : null; var description = normalized.TryGetValue("descricao", out var d) ? d.ToString() : null;
            return new ResultadoValidacaoLinha(item.NumeroLinha, status, code, description, converted, original, operation, null, erros, avisos, [], erros.Count == 0 && operation is TipoOperacaoImportacao.Inserir or TipoOperacaoImportacao.Atualizar, operation == TipoOperacaoImportacao.Atualizar);
        }).ToList();
        var result = new ResultadoValidacaoImportacao(historico.TotalLinhas, historico.LinhasValidas, historico.LinhasComErro, historico.LinhasComAviso,
            historico.ProdutosNovos, historico.ProdutosExistentes, historico.ProdutosAtualizaveis, historico.LinhasDuplicadas, historico.LinhasIgnoradas,
            historico.Status == StatusImportacao.ProntaParaImportar, linhas, historico.FinalizadoEm ?? historico.UpdatedAt ?? historico.CreatedAt,
            opcoes?.QuantidadeUnidadesPreenchidasAutomaticamente ?? 0);
        return new(result, linhas, pagina, pages, total, allIssues);
    }

    private static ErroValidacaoImportacao ToValidationIssue(ImportacaoErro issue) => new(issue.NumeroLinha ?? 0, issue.Coluna ?? string.Empty,
        issue.ValorOriginal, issue.Codigo, issue.Mensagem, issue.Severidade, issue.CreatedAt);
}
