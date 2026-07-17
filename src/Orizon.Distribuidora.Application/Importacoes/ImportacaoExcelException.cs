namespace Orizon.Distribuidora.Application.Importacoes;

public sealed class ImportacaoExcelException : Exception
{
    public ImportacaoExcelException(string message)
        : base(message)
    {
    }

    public ImportacaoExcelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
