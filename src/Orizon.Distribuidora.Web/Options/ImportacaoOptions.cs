namespace Orizon.Distribuidora.Web.Options;

public sealed class ImportacaoOptions
{
    public const string SectionName = "Importacao";

    public int TamanhoMaximoArquivoMB { get; set; } = 25;

    public string[] ExtensoesPermitidas { get; set; } = [];

    public int TamanhoAmostraPreview { get; set; } = 10;

    public long TamanhoMaximoArquivoBytes =>
        Math.Max(1, TamanhoMaximoArquivoMB) * 1024L * 1024L;

    public IReadOnlyList<string> GetExtensoesPermitidas() =>
        ExtensoesPermitidas.Length == 0
            ? [".xlsx"]
            : ExtensoesPermitidas
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(extension => extension.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
}
