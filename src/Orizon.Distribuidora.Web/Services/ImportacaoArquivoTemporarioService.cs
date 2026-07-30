using System.Text.Json;
using Microsoft.Extensions.Options;
using Orizon.Distribuidora.Web.Options;

namespace Orizon.Distribuidora.Web.Services;

public sealed record ImportacaoArquivoTemporario(
    string Token,
    Guid ImportacaoId,
    Guid EmpresaId,
    Guid? UsuarioId,
    string Caminho,
    DateTimeOffset ExpiraEm,
    IReadOnlyDictionary<string, string>? Mapeamentos = null,
    string? AbaSelecionada = null);

public sealed class ImportacaoArquivoTemporarioService(IOptions<ImportacaoOptions> options)
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "orizon-importacoes");
    private readonly TimeSpan lifetime = TimeSpan.FromMinutes(Math.Max(5, options.Value.ExpiracaoArquivoTemporarioMinutos));

    public async Task<ImportacaoArquivoTemporario> SalvarAsync(IFormFile arquivo, Guid importacaoId, Guid empresaId, Guid? usuarioId, CancellationToken ct)
    {
        LimparExpirados(); Directory.CreateDirectory(directory);
        var token = Guid.NewGuid().ToString("N"); var path = DataPath(token);
        await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            await arquivo.CopyToAsync(output, ct);
        var metadata = new ImportacaoArquivoTemporario(token, importacaoId, empresaId, usuarioId, path, DateTimeOffset.UtcNow.Add(lifetime));
        await File.WriteAllTextAsync(MetadataPath(token), JsonSerializer.Serialize(metadata), ct);
        return metadata;
    }

    public async Task<ImportacaoArquivoTemporario?> ObterAsync(string token, Guid importacaoId, Guid empresaId, Guid? usuarioId, CancellationToken ct)
    {
        if (!Guid.TryParseExact(token, "N", out _)) return null;
        var metadataPath = MetadataPath(token); if (!File.Exists(metadataPath)) return null;
        ImportacaoArquivoTemporario? metadata;
        try { metadata = JsonSerializer.Deserialize<ImportacaoArquivoTemporario>(await File.ReadAllTextAsync(metadataPath, ct)); }
        catch (Exception ex) when (ex is IOException or JsonException) { Excluir(token); return null; }
        if (metadata is null) { Excluir(token); return null; }
        if (metadata.ImportacaoId != importacaoId || metadata.EmpresaId != empresaId || metadata.UsuarioId != usuarioId) return null;
        if (metadata.ExpiraEm <= DateTimeOffset.UtcNow || !File.Exists(metadata.Caminho)) { Excluir(token); return null; }
        return metadata;
    }

    public async Task SalvarMapeamentoAsync(
        string token,
        Guid importacaoId,
        Guid empresaId,
        Guid? usuarioId,
        IReadOnlyDictionary<string, string> mapeamentos,
        string? abaSelecionada,
        CancellationToken ct)
    {
        var metadata = await ObterAsync(token, importacaoId, empresaId, usuarioId, ct)
            ?? throw new FileNotFoundException("O arquivo temporário não está mais disponível.");
        var atualizado = metadata with
        {
            Mapeamentos = new Dictionary<string, string>(mapeamentos, StringComparer.Ordinal),
            AbaSelecionada = abaSelecionada
        };
        await File.WriteAllTextAsync(MetadataPath(token), JsonSerializer.Serialize(atualizado), ct);
    }

    public void Excluir(string token)
    {
        if (!Guid.TryParseExact(token, "N", out _)) return;
        TryDelete(DataPath(token)); TryDelete(MetadataPath(token));
    }

    public void LimparExpirados()
    {
        if (!Directory.Exists(directory)) return;
        foreach (var metadataPath in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<ImportacaoArquivoTemporario>(File.ReadAllText(metadataPath));
                if (metadata is null || metadata.ExpiraEm <= DateTimeOffset.UtcNow) { if (metadata is not null) TryDelete(metadata.Caminho); TryDelete(metadataPath); }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        }
    }

    private string DataPath(string token) => Path.Combine(directory, $"{token}.xlsx");
    private string MetadataPath(string token) => Path.Combine(directory, $"{token}.json");
    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
