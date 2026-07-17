using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;

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

public sealed class ImportacaoMapeamentoViewModel
{
    public Guid ImportacaoId { get; set; }
    public string TokenArquivo { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;
    public string? AbaSelecionada { get; set; }
    public IReadOnlyList<string> Cabecalhos { get; set; } = [];
    public IReadOnlyList<ImportacaoLinhaAmostraViewModel> Amostra { get; set; } = [];
    public IReadOnlyList<CampoImportavel> Campos { get; set; } = [];
    public IReadOnlyDictionary<string, string> Mapeamentos { get; set; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, double> Confiancas { get; set; } = new Dictionary<string, double>();
    public IReadOnlyList<ModeloImportacaoDto> Modelos { get; set; } = [];
    public Guid? ModeloCarregadoId { get; set; }
}

public sealed class SalvarModeloImportacaoRequest
{
    public string Nome { get; set; } = string.Empty;
    public bool Padrao { get; set; }
    public Dictionary<string, string> Mapeamentos { get; set; } = [];
    public List<string> Cabecalhos { get; set; } = [];
    public List<Dictionary<string, string?>> Amostra { get; set; } = [];
}

public sealed class ExecutarValidacaoImportacaoRequest
{
    public Guid ImportacaoId { get; set; }
    public string TokenArquivo { get; set; } = string.Empty;
    public string? AbaSelecionada { get; set; }
    public string MapeamentoJson { get; set; } = "{}";
    public bool InserirNovos { get; set; } = true;
    public bool AtualizarExistentes { get; set; } = true;
    public bool IgnorarVaziosAtualizacao { get; set; } = true;
    public bool PermitirImportacaoParcial { get; set; } = true;
    public bool BloquearComQualquerErro { get; set; }
    public int Pagina { get; set; } = 1;
    public string? Filtro { get; set; }
    public string? Busca { get; set; }
    public bool PersistirResultado { get; set; } = true;
}

public sealed class ImportacaoValidacaoViewModel
{
    public Guid ImportacaoId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public string? AbaSelecionada { get; set; }
    public ResultadoValidacaoImportacao Resultado { get; set; } = ResultadoValidacaoImportacao.Sucesso;
    public IReadOnlyList<ResultadoValidacaoLinha> Linhas { get; set; } = [];
    public int Pagina { get; set; }
    public int TotalPaginas { get; set; }
    public string? Filtro { get; set; }
    public string? Busca { get; set; }
    public ExecutarValidacaoImportacaoRequest Request { get; set; } = new();
}
public sealed class ImportacaoResultadoViewModel
{
    public ResultadoExecucaoImportacao Resultado { get; set; } = null!;
    public IReadOnlyList<ResultadoExecucaoItem> Itens { get; set; }=[];
    public int Pagina { get; set; }
    public int TotalPaginas { get; set; }
    public string? Filtro { get; set; }
    public string? Busca { get; set; }
}
