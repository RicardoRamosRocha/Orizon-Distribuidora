using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class ModeloImportacaoService(ApplicationDbContext db) : IModeloImportacaoService
{
    public async Task<IReadOnlyList<ModeloImportacaoDto>> ListarAsync(Guid empresaId, Guid? usuarioId, CancellationToken cancellationToken = default) =>
        (await db.ModelosImportacao.AsNoTracking().Where(x => x.CompanyId == empresaId && x.Ativo && (x.UsuarioId == null || x.UsuarioId == usuarioId)).OrderByDescending(x => x.Padrao).ThenBy(x => x.Nome).ToListAsync(cancellationToken)).Select(ToDto).ToList();

    public async Task<ModeloImportacaoDto?> EncontrarCompativelAsync(Guid empresaId, Guid? usuarioId, IReadOnlyList<string> cabecalhos, CancellationToken cancellationToken = default)
    {
        var assinatura = Assinatura(cabecalhos);
        var model = await db.ModelosImportacao.AsNoTracking().Where(x => x.CompanyId == empresaId && x.Ativo && x.AssinaturaColunas == assinatura && (x.UsuarioId == null || x.UsuarioId == usuarioId)).OrderByDescending(x => x.Padrao).FirstOrDefaultAsync(cancellationToken);
        return model is null ? null : ToDto(model);
    }

    public async Task<ModeloImportacaoDto> SalvarAsync(Guid empresaId, Guid? usuarioId, string nome, bool padrao, IReadOnlyDictionary<string, string> mapeamentos, IReadOnlyList<string> cabecalhos, CancellationToken cancellationToken = default)
    {
        if (padrao) foreach (var item in await db.ModelosImportacao.Where(x => x.CompanyId == empresaId && x.Padrao).ToListAsync(cancellationToken)) item.DefinirComoPadrao(false);
        var model = new ModeloImportacao(empresaId, nome, TipoArquivoImportacao.Excel, JsonSerializer.Serialize(mapeamentos), usuarioId, Assinatura(cabecalhos), padrao);
        db.ModelosImportacao.Add(model); await db.SaveChangesAsync(cancellationToken); return ToDto(model);
    }

    public async Task ExcluirAsync(Guid empresaId, Guid id, CancellationToken cancellationToken = default)
    { var model = await db.ModelosImportacao.FirstOrDefaultAsync(x => x.CompanyId == empresaId && x.Id == id, cancellationToken) ?? throw new KeyNotFoundException(); db.ModelosImportacao.Remove(model); await db.SaveChangesAsync(cancellationToken); }

    private static string Assinatura(IEnumerable<string> colunas) => string.Join('|', colunas.Select(MapeadorColunasService.Normalizar).Order());
    private static ModeloImportacaoDto ToDto(ModeloImportacao x) => new(x.Id, x.Nome, x.UsuarioId, x.CreatedAt, x.Padrao, JsonSerializer.Deserialize<Dictionary<string, string>>(x.MapeamentoColunasJson) ?? []);
}
