using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Data;

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
}
