namespace Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Enums;

public enum StatusValidacaoLinha { Valida = 1, ComAviso, Invalida, Duplicada, Ignorada }
public enum TipoOperacaoImportacao { Inserir = 1, Atualizar, Ignorar, Bloquear }

public sealed record ErroValidacaoImportacao(int NumeroLinha, string Campo, string? ValorOriginal, string Codigo, string Mensagem, SeveridadeValidacao Severidade, DateTimeOffset CriadoEm);
public sealed record AlteracaoProdutoImportacao(string Campo, object? ValorAtual, string? ValorPlanilha, object? ValorConvertido, bool Alterado);
public sealed record ProdutoExistenteImportacao(Guid Id, string Codigo, string Descricao, string? CodigoBarras, decimal PrecoCusto, decimal PrecoVenda, Guid UnidadeId, bool Ativo);
public sealed record ResultadoValidacaoLinha(int NumeroLinha, StatusValidacaoLinha Status, string? CodigoProduto, string? Descricao,
    IReadOnlyDictionary<string, object?> ValoresConvertidos, IReadOnlyDictionary<string, string?> DadosOriginais,
    TipoOperacaoImportacao Operacao, ProdutoExistenteImportacao? ProdutoExistente,
    IReadOnlyList<ErroValidacaoImportacao> Erros, IReadOnlyList<ErroValidacaoImportacao> Avisos,
    IReadOnlyList<AlteracaoProdutoImportacao> Alteracoes, bool PodeImportar, bool PodeAtualizar);

public sealed record OpcoesValidacaoImportacao(bool InserirNovos = true, bool AtualizarExistentes = true, bool IgnorarVaziosAtualizacao = true,
    bool PermitirImportacaoParcial = true, bool BloquearComQualquerErro = false, bool CodigoCaseInsensitive = true);

public sealed record ResultadoValidacaoImportacao(int TotalLinhas, int QuantidadeValida, int QuantidadeComErro, int QuantidadeComAviso,
    int QuantidadeNovos, int QuantidadeExistentes, int QuantidadeAtualizaveis, int QuantidadeDuplicidades, int QuantidadeIgnoradas,
    bool PodeImportar, IReadOnlyList<ResultadoValidacaoLinha> Linhas, DateTimeOffset ValidadoEm)
{
    public bool Valido => QuantidadeComErro == 0;
    public IReadOnlyList<ErroImportacaoDto> Erros => Linhas.SelectMany(x => x.Erros).Select(x => new ErroImportacaoDto(x.NumeroLinha, x.Campo, x.Mensagem)).ToList();
    public static ResultadoValidacaoImportacao Sucesso { get; } = new(0,0,0,0,0,0,0,0,0,true,[],DateTimeOffset.UtcNow);
}

public sealed record ContextoValidacaoImportacao(Guid ImportacaoId, Guid EmpresaId, Guid? UsuarioId,
    IReadOnlyList<LinhaPlanilhaImportada> Linhas, MapeamentoColunasImportacao Mapeamento,
    OpcoesValidacaoImportacao Opcoes, IReadOnlyList<ProdutoExistenteImportacao> ProdutosExistentes,
    IReadOnlyDictionary<string, Guid> Unidades);
