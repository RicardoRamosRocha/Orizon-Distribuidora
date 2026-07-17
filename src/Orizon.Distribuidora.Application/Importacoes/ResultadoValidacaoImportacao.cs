namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record ResultadoValidacaoImportacao(
    bool Valido,
    IReadOnlyList<ErroImportacaoDto> Erros)
{
    public static ResultadoValidacaoImportacao Sucesso { get; } = new(true, []);
}
