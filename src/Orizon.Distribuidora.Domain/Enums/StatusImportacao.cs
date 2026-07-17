namespace Orizon.Distribuidora.Domain.Enums;

public enum StatusImportacao
{
    AguardandoProcessamento = 1,
    EmProcessamento = 2,
    ProcessadaComSucesso = 3,
    ProcessadaComErros = 4,
    Cancelada = 5,
    Validando = 6,
    ValidacaoConcluida = 7,
    ValidacaoComErros = 8,
    ProntaParaImportar = 9
}
