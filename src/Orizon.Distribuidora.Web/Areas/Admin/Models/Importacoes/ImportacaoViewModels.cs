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
