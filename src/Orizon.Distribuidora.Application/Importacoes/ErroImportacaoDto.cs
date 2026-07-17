namespace Orizon.Distribuidora.Application.Importacoes;

public sealed record ErroImportacaoDto(
    int? NumeroLinha,
    string? Coluna,
    string Mensagem,
    string? ValorOriginal = null);
