using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Orizon.Distribuidora.Web.Options;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ImportacaoUploadValidatorTests
{
    [Fact]
    public void Validate_DeveRejeitarArquivoVazio()
    {
        var validator = CreateValidator();
        using var stream = new MemoryStream();
        var file = new FormFile(stream, 0, 0, "arquivo", "produtos.xlsx");

        var result = validator.Validate(file);

        Assert.False(result.IsValid);
        Assert.Contains("vazio", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_DeveRejeitarExtensaoInvalida()
    {
        var validator = CreateValidator();
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "arquivo", "produtos.csv");

        var result = validator.Validate(file);

        Assert.False(result.IsValid);
        Assert.Contains("formato", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_DeveAceitarXlsxDentroDoLimite()
    {
        var validator = CreateValidator();
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "arquivo", @"C:\fakepath\produtos.xlsx");

        var result = validator.Validate(file);

        Assert.True(result.IsValid);
        Assert.Equal("produtos.xlsx", result.SanitizedFileName);
    }

    private static ImportacaoUploadValidator CreateValidator()
    {
        return new ImportacaoUploadValidator(Options.Create(new ImportacaoOptions
        {
            TamanhoMaximoArquivoMB = 25,
            ExtensoesPermitidas = [".xlsx"]
        }));
    }
}
