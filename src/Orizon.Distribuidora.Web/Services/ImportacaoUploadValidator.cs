using Microsoft.Extensions.Options;
using Orizon.Distribuidora.Web.Options;

namespace Orizon.Distribuidora.Web.Services;

public sealed class ImportacaoUploadValidationResult
{
    private ImportacaoUploadValidationResult(bool isValid, string? errorMessage, string sanitizedFileName)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
        SanitizedFileName = sanitizedFileName;
    }

    public bool IsValid { get; }

    public string? ErrorMessage { get; }

    public string SanitizedFileName { get; }

    public static ImportacaoUploadValidationResult Success(string sanitizedFileName) =>
        new(true, null, sanitizedFileName);

    public static ImportacaoUploadValidationResult Failure(string errorMessage) =>
        new(false, errorMessage, string.Empty);
}

public sealed class ImportacaoUploadValidator
{
    private readonly ImportacaoOptions options;

    public ImportacaoUploadValidator(IOptions<ImportacaoOptions> options)
    {
        this.options = options.Value;
    }

    public ImportacaoUploadValidationResult Validate(IFormFile? arquivo)
    {
        if (arquivo is null)
        {
            return ImportacaoUploadValidationResult.Failure("Selecione um arquivo Excel para continuar.");
        }

        if (arquivo.Length == 0)
        {
            return ImportacaoUploadValidationResult.Failure("O arquivo selecionado está vazio.");
        }

        if (arquivo.Length > options.TamanhoMaximoArquivoBytes)
        {
            return ImportacaoUploadValidationResult.Failure($"O arquivo ultrapassa o limite de {options.TamanhoMaximoArquivoMB} MB.");
        }

        var fileName = SanitizeFileName(arquivo.FileName);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ImportacaoUploadValidationResult.Failure("O nome do arquivo é inválido.");
        }

        if (fileName.Length > 260)
        {
            return ImportacaoUploadValidationResult.Failure("O nome do arquivo é muito longo.");
        }

        var extension = Path.GetExtension(fileName);

        var allowedExtensions = options.GetExtensoesPermitidas();

        if (!allowedExtensions.Any(
                allowed => string.Equals(allowed, extension, StringComparison.OrdinalIgnoreCase)))
        {
            return ImportacaoUploadValidationResult.Failure($"Formato não permitido. Envie um arquivo {string.Join(", ", allowedExtensions)}.");
        }

        return ImportacaoUploadValidationResult.Success(fileName);
    }

    public static string SanitizeFileName(string fileName)
    {
        var normalized = (fileName ?? string.Empty).Replace('\\', '/');
        var safeName = Path.GetFileName(normalized);

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidChar, '_');
        }

        return safeName.Trim();
    }
}
