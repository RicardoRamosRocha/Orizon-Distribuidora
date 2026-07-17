using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record ArquivoImportacaoExcel(
    Stream Conteudo,
    string NomeArquivo,
    long TamanhoBytes,
    TipoArquivoImportacao TipoArquivo = TipoArquivoImportacao.Excel);
