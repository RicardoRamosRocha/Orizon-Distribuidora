using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Application.Stock;

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
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Conflitos { get; set; } =
        new Dictionary<string, IReadOnlyList<string>>();
    public IReadOnlyList<ErroMapeamento> ErrosMapeamento { get; set; } = [];
    public IReadOnlyList<ModeloImportacaoDto> Modelos { get; set; } = [];
    public Guid? ModeloCarregadoId { get; set; }
    public IReadOnlyList<StockOptionDto> Depositos { get; set; } = [];
    public IReadOnlyList<StockOptionDto> LocaisInternos { get; set; } = [];
    public IReadOnlyList<ImportacaoReconhecimentoColunaViewModel> Reconhecimentos { get; set; } = [];
    public int ColunasReconhecidas => Reconhecimentos.Count(item => !string.IsNullOrWhiteSpace(item.CampoDestino));
    public int ColunasAprendidas => Reconhecimentos.Count(item => item.Aprendido);
    public int ColunasPendentes => Reconhecimentos.Count(item => item.ExigeRevisao);
    public double ConfiancaGeral => ColunasReconhecidas == 0
        ? 0
        : Reconhecimentos.Where(item => !string.IsNullOrWhiteSpace(item.CampoDestino)).Average(item => item.Confianca);
}

public sealed class ImportacaoReconhecimentoColunaViewModel
{
    public string Cabecalho { get; set; } = string.Empty;
    public string? CampoDestino { get; set; }
    public string? CampoNome { get; set; }
    public RecognitionStrategy? Estrategia { get; set; }
    public double Confianca { get; set; }
    public bool Aprendido { get; set; }
    public bool ExigeRevisao { get; set; }
    public IReadOnlyList<string> Conflitos { get; set; } = [];
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
    public Dictionary<string, string> Mapeamentos { get; set; } = [];
    public Guid? ModeloImportacaoId { get; set; }
    public Guid? DepositoId { get; set; }
    public Guid? LocalInternoId { get; set; }
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
    public string? TokenArquivo { get; set; }
    public string? AbaSelecionada { get; set; }
    public ResultadoValidacaoImportacao Resultado { get; set; } = ResultadoValidacaoImportacao.Sucesso;
    public IReadOnlyList<ResultadoValidacaoLinha> Linhas { get; set; } = [];
    public IReadOnlyList<ProblemaValidacaoViewModel> Problemas { get; set; } = [];
    public int Pagina { get; set; }
    public int TotalPaginas { get; set; }
    public string? Filtro { get; set; }
    public string? Busca { get; set; }
    public ExecutarValidacaoImportacaoRequest Request { get; set; } = new();
    public IReadOnlyDictionary<string, string> Mapeamentos { get; set; } =
        new Dictionary<string, string>();
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
public sealed class HistoricoImportacaoViewModel
{
    public ConsultaHistoricoImportacao Consulta { get; set; } = new();
    public PaginaHistoricoImportacao Pagina { get; set; } = null!;
}
public sealed class HistoricoImportacaoDetalhesViewModel
{
    public HistoricoImportacaoDetalhesDto Detalhes { get; set; } = null!;
}
public sealed class HistoricoDashboardViewModel
{
    public HistoricoDashboardDto Dashboard { get; set; } = null!;
}
public sealed class RollbackConfirmacaoViewModel
{
    public AnaliseRollbackImportacao Analise { get; set; } = null!;
}
public sealed class RollbackResultadoViewModel
{
    public ResultadoRollbackImportacao Resultado { get; set; } = null!;
}
