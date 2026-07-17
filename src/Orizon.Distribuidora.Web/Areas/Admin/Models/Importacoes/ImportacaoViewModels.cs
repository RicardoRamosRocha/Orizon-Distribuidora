using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;

public sealed class ImportacaoIndexViewModel
{
    public int TotalImportacoes { get; set; }

    public int ImportacoesComErro { get; set; }

    public int LinhasImportadas { get; set; }

    public IReadOnlyList<ImportacaoHistoricoResumoViewModel> UltimasImportacoes { get; set; } = [];
}

public sealed class ImportacaoHistoricoResumoViewModel
{
    public Guid Id { get; set; }

    public string NomeArquivo { get; set; } = string.Empty;

    public StatusImportacao Status { get; set; }

    public int TotalLinhas { get; set; }

    public int LinhasComErro { get; set; }

    public int LinhasImportadas { get; set; }

    public DateTimeOffset CriadoEm { get; set; }
}

public sealed class ImportacaoUploadViewModel
{
    public int TamanhoMaximoArquivoMB { get; set; }

    public string FormatosAceitos { get; set; } = ".xlsx";
}

public sealed class ImportacaoPreviewViewModel
{
    public Guid ImportacaoId { get; set; }

    public string TokenArquivo { get; set; } = string.Empty;

    public string NomeArquivo { get; set; } = string.Empty;

    public long TamanhoArquivoBytes { get; set; }

    public string? AbaSelecionada { get; set; }

    public IReadOnlyList<ImportacaoAbaViewModel> Abas { get; set; } = [];

    public IReadOnlyList<string> Cabecalhos { get; set; } = [];

    public int QuantidadeLinhas { get; set; }

    public IReadOnlyList<ImportacaoLinhaAmostraViewModel> Amostra { get; set; } = [];

    public bool PossuiMultiplasAbas => Abas.Count > 1;

    public string TamanhoFormatado => FormatFileSize(TamanhoArquivoBytes);

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024L * 1024L)
        {
            return $"{bytes / 1024d / 1024d:0.##} MB";
        }

        return $"{bytes / 1024d:0.##} KB";
    }
}

public sealed class ImportacaoAbaViewModel
{
    public string Nome { get; set; } = string.Empty;

    public int QuantidadeLinhas { get; set; }

    public int QuantidadeColunas { get; set; }
}

public sealed class ImportacaoLinhaAmostraViewModel
{
    public int NumeroLinha { get; set; }

    public IReadOnlyDictionary<string, string?> Valores { get; set; } =
        new Dictionary<string, string?>();
}
