namespace Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Enums;

public enum StatusValidacaoLinha { Valida = 1, ComAviso, Invalida, Duplicada, Ignorada }
public enum TipoOperacaoImportacao { Inserir = 1, Atualizar, Ignorar, Bloquear }

public sealed record ErroValidacaoImportacao(int NumeroLinha, string Campo, string? ValorOriginal, string Codigo, string Mensagem, SeveridadeValidacao Severidade, DateTimeOffset CriadoEm);
public sealed record AlteracaoProdutoImportacao(string Campo, object? ValorAtual, string? ValorPlanilha, object? ValorConvertido, bool Alterado);
public sealed record ProdutoExistenteImportacao(Guid Id, string Codigo, string Descricao, string? CodigoBarras, decimal PrecoCusto, decimal PrecoVenda, Guid UnidadeId, bool Ativo,
    ProductType Tipo = ProductType.Own, Guid? ParceiroId = null, bool ControlaEstoque = true,
    string? DescricaoComplementar = null, Guid? CategoriaId = null, Guid? SubcategoriaId = null,
    Guid? MarcaId = null, Guid? GrupoId = null, Guid? FornecedorId = null,
    Guid? DepositoPadraoId = null, Guid? LocalInternoPadraoId = null,
    string? Ncm = null, string? Observacoes = null, decimal? EstoqueMinimo = null);
public sealed record ResultadoValidacaoLinha(int NumeroLinha, StatusValidacaoLinha Status, string? CodigoProduto, string? Descricao,
    IReadOnlyDictionary<string, object?> ValoresConvertidos, IReadOnlyDictionary<string, string?> DadosOriginais,
    TipoOperacaoImportacao Operacao, ProdutoExistenteImportacao? ProdutoExistente,
    IReadOnlyList<ErroValidacaoImportacao> Erros, IReadOnlyList<ErroValidacaoImportacao> Avisos,
    IReadOnlyList<AlteracaoProdutoImportacao> Alteracoes, bool PodeImportar, bool PodeAtualizar);

public sealed record OpcoesValidacaoImportacao(bool InserirNovos = true, bool AtualizarExistentes = true, bool IgnorarVaziosAtualizacao = true,
    bool PermitirImportacaoParcial = true, bool BloquearComQualquerErro = false, bool CodigoCaseInsensitive = true,
    Guid? DepositoId = null, Guid? LocalInternoId = null,
    IReadOnlyDictionary<string, string>? Mapeamentos = null, string? AbaSelecionada = null,
    int QuantidadeUnidadesPreenchidasAutomaticamente = 0);

public sealed record ResultadoValidacaoImportacao(int TotalLinhas, int QuantidadeValida, int QuantidadeComErro, int QuantidadeComAviso,
    int QuantidadeNovos, int QuantidadeExistentes, int QuantidadeAtualizaveis, int QuantidadeDuplicidades, int QuantidadeIgnoradas,
    bool PodeImportar, IReadOnlyList<ResultadoValidacaoLinha> Linhas, DateTimeOffset ValidadoEm,
    int QuantidadeUnidadesPreenchidasAutomaticamente = 0)
{
    public bool Valido => QuantidadeComErro == 0;
    public IReadOnlyList<ErroImportacaoDto> Erros => Linhas.SelectMany(x => x.Erros).Select(x => new ErroImportacaoDto(x.NumeroLinha, x.Campo, x.Mensagem)).ToList();
    public static ResultadoValidacaoImportacao Sucesso { get; } = new(0,0,0,0,0,0,0,0,0,true,[],DateTimeOffset.UtcNow);
}

public sealed record PaginaValidacaoImportacao(ResultadoValidacaoImportacao Resultado,
    IReadOnlyList<ResultadoValidacaoLinha> Linhas, int Pagina, int TotalPaginas, int TotalFiltrado,
    IReadOnlyList<ErroValidacaoImportacao>? Ocorrencias = null);

public sealed record ReferenciaImportacao(Guid Id, string? Codigo, string Nome, string? Documento = null, Guid? ParentId = null);

public sealed record ReferenciasProdutoImportacao(
    IReadOnlyList<ReferenciaImportacao> Unidades,
    IReadOnlyList<ReferenciaImportacao> Marcas,
    IReadOnlyList<ReferenciaImportacao> Categorias,
    IReadOnlyList<ReferenciaImportacao> Subcategorias,
    IReadOnlyList<ReferenciaImportacao> Grupos,
    IReadOnlyList<ReferenciaImportacao> Fornecedores,
    IReadOnlyList<ReferenciaImportacao> Parceiros,
    bool DepositoValido,
    bool LocalInternoValido);

public sealed record ContextoValidacaoImportacao(Guid ImportacaoId, Guid EmpresaId, Guid? UsuarioId,
    IReadOnlyList<LinhaPlanilhaImportada> Linhas, MapeamentoColunasImportacao Mapeamento,
    OpcoesValidacaoImportacao Opcoes, IReadOnlyList<ProdutoExistenteImportacao> ProdutosExistentes,
    ReferenciasProdutoImportacao Referencias, int QuantidadeUnidadesPreenchidasAutomaticamente = 0,
    IReadOnlySet<Guid>? ProdutosComSaldoInicialNoDeposito = null);
