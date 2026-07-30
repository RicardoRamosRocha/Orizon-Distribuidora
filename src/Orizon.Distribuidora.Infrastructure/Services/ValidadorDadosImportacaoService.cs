using System.Globalization;
using System.Text;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class ValidadorDadosImportacaoService : IValidadorDadosImportacaoService
{
    public Task<ResultadoValidacaoImportacao> ValidarAsync(ContextoValidacaoImportacao contexto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var produtosCodigo = contexto.ProdutosExistentes.ToDictionary(x => NormalizarCodigo(x.Codigo), StringComparer.Ordinal);
        var produtosBarras = contexto.ProdutosExistentes.Where(x => !string.IsNullOrWhiteSpace(x.CodigoBarras)).ToDictionary(x => x.CodigoBarras!, StringComparer.Ordinal);
        var uteis = contexto.Linhas.Where(x => x.Valores.Values.Any(v => !string.IsNullOrWhiteSpace(v))).ToList();
        var codigos = Duplicados(uteis, contexto.Mapeamento, "codigo", NormalizarCodigo);
        var barras = Duplicados(uteis, contexto.Mapeamento, "codigoBarras", NormalizarBarras);
        var resultados = new List<ResultadoValidacaoLinha>(uteis.Count);

        foreach (var linha in uteis)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var erros = new List<ErroValidacaoImportacao>();
            var avisos = new List<ErroValidacaoImportacao>();
            var convertidos = new Dictionary<string, object?>();
            var codigo = NormalizarCodigo(Valor(linha, contexto.Mapeamento, "codigo"));
            var descricao = Texto(linha, contexto.Mapeamento, "descricao");
            var barcode = NormalizarBarras(Valor(linha, contexto.Mapeamento, "codigoBarras"));
            var unidade = Texto(linha, contexto.Mapeamento, "unidade");
            var vendaOriginal = Valor(linha, contexto.Mapeamento, "precoVenda");
            Obrigatorio(codigo, "codigo", "Código", linha, erros);
            Obrigatorio(descricao, "descricao", "Descrição", linha, erros);
            Obrigatorio(unidade, "unidade", "Unidade", linha, erros);
            Obrigatorio(vendaOriginal, "precoVenda", "Preço de venda", linha, erros);
            Limite(linha, erros, "codigo", codigo, 50);
            Limite(linha, erros, "descricao", descricao, 200);
            if (barcode.Length > 32 || (barcode.Length > 0 && barcode.Any(x => !char.IsDigit(x))))
                Erro(linha, erros, "codigoBarras", barcode, "IMP_CODIGO_BARRAS_INVALIDO", "O código de barras deve conter até 32 dígitos.");
            if (codigos.TryGetValue(codigo, out var linhasCodigo))
                Erro(linha, erros, "codigo", codigo, "IMP_DUPLICIDADE_CODIGO_PLANILHA", $"Código duplicado nas linhas {string.Join(", ", linhasCodigo)}.");
            if (barcode.Length > 0 && barras.TryGetValue(barcode, out var linhasBarras))
                Erro(linha, erros, "codigoBarras", barcode, "IMP_DUPLICIDADE_BARRAS_PLANILHA", $"Código de barras duplicado nas linhas {string.Join(", ", linhasBarras)}.");

            Add(convertidos, "codigo", codigo);
            Add(convertidos, "descricao", descricao);
            Add(convertidos, "codigoBarras", barcode.Length == 0 ? null : barcode);
            AddTexto(convertidos, linha, contexto.Mapeamento, erros, "descricaoComplementar", 4000);
            AddTexto(convertidos, linha, contexto.Mapeamento, erros, "ncm", 10);
            if (convertidos.TryGetValue("ncm", out var ncmValue))
            {
                var ncm = string.Concat(Convert.ToString(ncmValue, CultureInfo.InvariantCulture)!.Where(char.IsDigit));
                if (ncm.Length != 8) Erro(linha, erros, "ncm", Convert.ToString(ncmValue), "IMP_NCM_INVALIDO", "O NCM deve possuir exatamente 8 dígitos.");
                else convertidos["ncm"] = ncm;
            }
            AddTexto(convertidos, linha, contexto.Mapeamento, erros, "observacoes", 2000);

            var custo = Decimal(linha, contexto.Mapeamento, "precoCompra", erros);
            var venda = Decimal(linha, contexto.Mapeamento, "precoVenda", erros);
            var estoque = Decimal(linha, contexto.Mapeamento, "estoqueInicial", erros);
            Add(convertidos, "precoCompra", custo);
            Add(convertidos, "precoVenda", venda);
            Add(convertidos, "estoqueInicial", estoque);
            if (custo < 0) Erro(linha, erros, "precoCompra", custo?.ToString(), "IMP_VALOR_NEGATIVO", "O preço de custo não pode ser negativo.");
            if (venda < 0) Erro(linha, erros, "precoVenda", venda?.ToString(), "IMP_VALOR_NEGATIVO", "O preço de venda não pode ser negativo.");
            if (estoque < 0) Erro(linha, erros, "estoqueInicial", estoque?.ToString(), "IMP_VALOR_NEGATIVO", "O estoque inicial não pode ser negativo.");
            if (venda == 0) Aviso(linha, avisos, "precoVenda", venda?.ToString(), "IMP_PRECO_ZERADO", "O preço de venda está zerado.");
            if (venda.HasValue && custo.HasValue && venda < custo) Aviso(linha, avisos, "precoVenda", venda.ToString(), "IMP_PRECO_MENOR_CUSTO", "O preço de venda é menor que o custo.");

            produtosCodigo.TryGetValue(codigo, out var existente);
            if (existente is null && barcode.Length > 0 && produtosBarras.TryGetValue(barcode, out var conflito))
                Erro(linha, erros, "codigoBarras", barcode, "IMP_CODIGO_BARRAS_EXISTENTE", $"Código de barras já pertence ao produto {conflito.Codigo}.");

            ResolverReferencia(linha, contexto.Mapeamento, "unidade", unidade, contexto.Referencias.Unidades, convertidos, "unidadeId", erros);
            ResolverReferencia(linha, contexto.Mapeamento, "marca", Texto(linha, contexto.Mapeamento, "marca"), contexto.Referencias.Marcas, convertidos, "marcaId", erros);
            ResolverReferencia(linha, contexto.Mapeamento, "categoria", Texto(linha, contexto.Mapeamento, "categoria"), contexto.Referencias.Categorias, convertidos, "categoriaId", erros);
            ResolverReferencia(linha, contexto.Mapeamento, "grupo", Texto(linha, contexto.Mapeamento, "grupo"), contexto.Referencias.Grupos, convertidos, "grupoId", erros);
            ResolverDocumentoOuNome(linha, contexto.Mapeamento, "fornecedor", Texto(linha, contexto.Mapeamento, "fornecedor"), contexto.Referencias.Fornecedores, convertidos, "fornecedorId", erros);
            ResolverDocumento(linha, contexto.Mapeamento, "parceiroCnpj", Texto(linha, contexto.Mapeamento, "parceiroCnpj"), contexto.Referencias.Parceiros, convertidos, "parceiroId", erros);
            ResolverReferencia(linha, contexto.Mapeamento, "subcategoria", Texto(linha, contexto.Mapeamento, "subcategoria"), contexto.Referencias.Subcategorias, convertidos, "subcategoriaId", erros);
            if (convertidos.TryGetValue("categoriaId", out var categoriaId) && convertidos.TryGetValue("subcategoriaId", out var subcategoriaId))
            {
                var sub = contexto.Referencias.Subcategorias.First(x => x.Id.Equals(subcategoriaId));
                if (sub.ParentId != (Guid?)categoriaId)
                    Erro(linha, erros, "subcategoria", Valor(linha, contexto.Mapeamento, "subcategoria"), "IMP_RELACIONAMENTO_INCOMPATIVEL", "A subcategoria não pertence à categoria informada.");
            }

            var tipo = ResolverTipo(linha, contexto.Mapeamento, existente?.Tipo ?? ProductType.Own, erros);
            convertidos["tipoProduto"] = tipo == ProductType.ThirdParty ? "Terceiro" : "Próprio";
            var controlaEstoque = Booleano(linha, contexto.Mapeamento, "controlaEstoque", erros) ?? existente?.ControlaEstoque ?? true;
            var ativo = Status(linha, contexto.Mapeamento, erros) ?? existente?.Ativo ?? true;
            if (tipo == ProductType.ThirdParty)
            {
                controlaEstoque = false;
                if (estoque.HasValue && estoque.Value != 0)
                    Erro(linha, erros, "estoqueInicial", estoque.ToString(), "IMP_TERCEIRO_COM_ESTOQUE", "Produto de terceiro não pode receber estoque físico.");
                var parceiroId = convertidos.TryGetValue("parceiroId", out var novoParceiro) ? (Guid?)novoParceiro : existente?.ParceiroId;
                if (!parceiroId.HasValue)
                    Erro(linha, erros, "parceiroCnpj", Valor(linha, contexto.Mapeamento, "parceiroCnpj"), "IMP_PARCEIRO_OBRIGATORIO", "Produto de terceiro exige parceiro comercial ativo da mesma empresa.");
            }
            if (estoque > 0)
            {
                if (!contexto.Opcoes.DepositoId.HasValue || !contexto.Opcoes.LocalInternoId.HasValue)
                    Erro(linha, erros, "estoqueInicial", estoque.ToString(), "IMP_LOCAL_ESTOQUE_OBRIGATORIO", "Selecione um depósito e um local interno válidos antes da execução.");
                else if (!contexto.Referencias.DepositoValido || !contexto.Referencias.LocalInternoValido)
                    Erro(linha, erros, "estoqueInicial", estoque.ToString(), "IMP_LOCAL_ESTOQUE_INVALIDO", "O depósito/local selecionado não pertence à empresa ou está inativo.");
            }
            convertidos["controlaEstoque"] = controlaEstoque;
            convertidos["status"] = ativo;

            var alteracoes = existente is null ? [] : Comparar(existente, descricao, barcode, custo, venda,
                convertidos.TryGetValue("unidadeId", out var uid) ? uid : null, contexto.Opcoes.IgnorarVaziosAtualizacao);
            var operacao = erros.Count > 0 ? TipoOperacaoImportacao.Bloquear : existente is null
                ? (contexto.Opcoes.InserirNovos ? TipoOperacaoImportacao.Inserir : TipoOperacaoImportacao.Ignorar)
                : alteracoes.Count > 0 && contexto.Opcoes.AtualizarExistentes ? TipoOperacaoImportacao.Atualizar : TipoOperacaoImportacao.Ignorar;
            var status = erros.Any(x => x.Codigo.StartsWith("IMP_DUPLICIDADE", StringComparison.Ordinal)) ? StatusValidacaoLinha.Duplicada
                : erros.Count > 0 ? StatusValidacaoLinha.Invalida : avisos.Count > 0 ? StatusValidacaoLinha.ComAviso
                : operacao == TipoOperacaoImportacao.Ignorar ? StatusValidacaoLinha.Ignorada : StatusValidacaoLinha.Valida;
            resultados.Add(new(linha.NumeroLinha, status, codigo, descricao, convertidos, linha.Valores, operacao, existente,
                erros, avisos, alteracoes, erros.Count == 0 && operacao is TipoOperacaoImportacao.Inserir or TipoOperacaoImportacao.Atualizar,
                operacao == TipoOperacaoImportacao.Atualizar));
        }

        var ignored = contexto.Linhas.Count - uteis.Count;
        var invalid = resultados.Count(x => !x.PodeImportar && x.Status is StatusValidacaoLinha.Invalida or StatusValidacaoLinha.Duplicada);
        var valid = resultados.Count(x => x.PodeImportar);
        var canImport = valid > 0 && (contexto.Opcoes.PermitirImportacaoParcial || invalid == 0) && (!contexto.Opcoes.BloquearComQualquerErro || invalid == 0);
        return Task.FromResult(new ResultadoValidacaoImportacao(contexto.Linhas.Count, valid, invalid, resultados.Count(x => x.Avisos.Count > 0),
            resultados.Count(x => x.Operacao == TipoOperacaoImportacao.Inserir), resultados.Count(x => x.ProdutoExistente is not null),
            resultados.Count(x => x.Operacao == TipoOperacaoImportacao.Atualizar), resultados.Count(x => x.Status == StatusValidacaoLinha.Duplicada),
            ignored, canImport, resultados, DateTimeOffset.UtcNow));
    }

    public static string NormalizarCodigo(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
    public static string NormalizarTexto(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        return string.Concat(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
    }
    private static string NormalizarBarras(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    private static string NormalizarDocumento(string? value) => string.Concat((value ?? string.Empty).Where(char.IsDigit));
    private static string? Texto(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, string campo) => Valor(l, m, campo)?.Trim();
    private static string? Valor(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, string campo) => m.Colunas.TryGetValue(campo, out var c) && l.Valores.TryGetValue(c, out var v) ? v : null;
    private static Dictionary<string, int[]> Duplicados(IReadOnlyList<LinhaPlanilhaImportada> linhas, MapeamentoColunasImportacao m, string campo, Func<string?, string> normalizar) =>
        linhas.GroupBy(x => normalizar(Valor(x, m, campo)), StringComparer.Ordinal).Where(x => x.Key.Length > 0 && x.Count() > 1).ToDictionary(x => x.Key, x => x.Select(l => l.NumeroLinha).ToArray());
    private static void Obrigatorio(string? v, string campo, string nome, LinhaPlanilhaImportada l, List<ErroValidacaoImportacao> e) { if (string.IsNullOrWhiteSpace(v)) Erro(l, e, campo, v, "IMP_CAMPO_OBRIGATORIO", $"{nome} é obrigatório."); }
    private static void Limite(LinhaPlanilhaImportada l, List<ErroValidacaoImportacao> e, string campo, string? valor, int max) { if ((valor?.Length ?? 0) > max) Erro(l, e, campo, valor, "IMP_VALOR_FORA_LIMITE", $"O campo deve possuir no máximo {max} caracteres."); }
    private static decimal? Decimal(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, string campo, List<ErroValidacaoImportacao> e) { var v = Valor(l, m, campo); if (string.IsNullOrWhiteSpace(v)) return null; if (TryDecimal(v, out var d)) return d; Erro(l, e, campo, v, "IMP_NUMERO_INVALIDO", $"'{v}' não é um número válido."); return null; }
    public static bool TryDecimal(string value, out decimal result) { value = value.Trim(); var culture = value.Contains(',') ? new CultureInfo("pt-BR") : CultureInfo.InvariantCulture; return decimal.TryParse(value, NumberStyles.Number, culture, out result); }
    private static bool? Booleano(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, string campo, List<ErroValidacaoImportacao> erros)
    {
        var valor = Valor(l, m, campo); if (string.IsNullOrWhiteSpace(valor)) return null;
        var n = NormalizarTexto(valor); if (n is "sim" or "s" or "true" or "1" or "ativo") return true; if (n is "nao" or "n" or "false" or "0" or "inativo") return false;
        Erro(l, erros, campo, valor, "IMP_BOOLEANO_INVALIDO", "Use Sim/Não, Verdadeiro/Falso ou 1/0."); return null;
    }
    private static bool? Status(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, List<ErroValidacaoImportacao> erros) => Booleano(l, m, "status", erros);
    private static ProductType ResolverTipo(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, ProductType fallback, List<ErroValidacaoImportacao> erros)
    {
        var tipoTexto = Texto(l, m, "tipoProduto"); var proprio = Booleano(l, m, "produtoProprio", erros); var terceiro = Booleano(l, m, "produtoTerceiro", erros);
        if (proprio == true && terceiro == true) { Erro(l, erros, "tipoProduto", tipoTexto, "IMP_TIPO_CONFLITANTE", "Produto próprio e produto terceiro não podem estar ativos ao mesmo tempo."); return fallback; }
        if (terceiro == true || proprio == false) return ProductType.ThirdParty; if (proprio == true || terceiro == false) return ProductType.Own;
        if (string.IsNullOrWhiteSpace(tipoTexto)) return fallback;
        var n = NormalizarTexto(tipoTexto); if (n is "terceiro" or "thirdparty") return ProductType.ThirdParty; if (n is "proprio" or "own") return ProductType.Own;
        Erro(l, erros, "tipoProduto", tipoTexto, "IMP_TIPO_INVALIDO", "Tipo de produto inválido. Use Próprio ou Terceiro."); return fallback;
    }
    private static void AddTexto(Dictionary<string, object?> d, LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, List<ErroValidacaoImportacao> e, string campo, int max) { var v = Texto(l, m, campo); Limite(l, e, campo, v, max); Add(d, campo, v); }
    private static void Add(Dictionary<string, object?> d, string key, object? value) { if (value is not null && (value is not string s || s.Length > 0)) d[key] = value; }
    private static void ResolverReferencia(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, string campo, string? valor, IReadOnlyList<ReferenciaImportacao> refs, Dictionary<string, object?> d, string destino, List<ErroValidacaoImportacao> erros)
    {
        if (string.IsNullOrWhiteSpace(valor)) return; var n = NormalizarTexto(valor);
        var porCodigo = refs.Where(x => !string.IsNullOrWhiteSpace(x.Codigo) && NormalizarTexto(x.Codigo!) == n).Select(x => x.Id).Distinct().ToList();
        var matches = porCodigo.Count > 0 ? porCodigo : refs.Where(x => NormalizarTexto(x.Nome) == n).Select(x => x.Id).Distinct().ToList();
        ResolverResultado(l, campo, valor, matches, d, destino, erros);
    }
    private static void ResolverDocumentoOuNome(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, string campo, string? valor, IReadOnlyList<ReferenciaImportacao> refs, Dictionary<string, object?> d, string destino, List<ErroValidacaoImportacao> erros)
    {
        if (string.IsNullOrWhiteSpace(valor)) return; var doc = NormalizarDocumento(valor);
        var matches = doc.Length is 11 or 14 ? refs.Where(x => NormalizarDocumento(x.Documento) == doc).Select(x => x.Id).Distinct().ToList()
            : refs.Where(x => NormalizarTexto(x.Nome) == NormalizarTexto(valor)).Select(x => x.Id).Distinct().ToList();
        ResolverResultado(l, campo, valor, matches, d, destino, erros);
    }
    private static void ResolverDocumento(LinhaPlanilhaImportada l, MapeamentoColunasImportacao m, string campo, string? valor, IReadOnlyList<ReferenciaImportacao> refs, Dictionary<string, object?> d, string destino, List<ErroValidacaoImportacao> erros)
    {
        if (string.IsNullOrWhiteSpace(valor)) return; var doc = NormalizarDocumento(valor);
        var matches = refs.Where(x => NormalizarDocumento(x.Documento) == doc && doc.Length == 14).Select(x => x.Id).Distinct().ToList();
        ResolverResultado(l, campo, valor, matches, d, destino, erros);
    }
    private static void ResolverResultado(LinhaPlanilhaImportada l, string campo, string valor, IReadOnlyList<Guid> matches, Dictionary<string, object?> d, string destino, List<ErroValidacaoImportacao> erros)
    {
        if (matches.Count == 1) d[destino] = matches[0];
        else if (matches.Count == 0) Erro(l, erros, campo, valor, "IMP_CADASTRO_INEXISTENTE", $"Valor '{valor}' não foi encontrado entre os cadastros ativos da empresa.");
        else Erro(l, erros, campo, valor, "IMP_CADASTRO_AMBIGUO", $"Valor '{valor}' corresponde a mais de um cadastro. Informe um código ou documento inequívoco.");
    }
    private static void Erro(LinhaPlanilhaImportada l, List<ErroValidacaoImportacao> e, string c, string? v, string code, string msg) => e.Add(new(l.NumeroLinha, c, Limitar(v), code, msg, SeveridadeValidacao.Erro, DateTimeOffset.UtcNow));
    private static void Aviso(LinhaPlanilhaImportada l, List<ErroValidacaoImportacao> e, string c, string? v, string code, string msg) => e.Add(new(l.NumeroLinha, c, Limitar(v), code, msg, SeveridadeValidacao.Aviso, DateTimeOffset.UtcNow));
    private static string? Limitar(string? v) => v?.Length > 500 ? v[..500] : v;
    private static List<AlteracaoProdutoImportacao> Comparar(ProdutoExistenteImportacao p, string? nome, string barcode, decimal? custo, decimal? venda, object? unidade, bool ignorarVazio) { var a = new List<AlteracaoProdutoImportacao>(); AddAlteracao(a, "Descrição", p.Descricao, nome, nome, ignorarVazio); AddAlteracao(a, "Código de barras", p.CodigoBarras, barcode, barcode, ignorarVazio); AddAlteracao(a, "Preço Compra", p.PrecoCusto, custo?.ToString(), custo, ignorarVazio); AddAlteracao(a, "Preço Venda", p.PrecoVenda, venda?.ToString(), venda, ignorarVazio); AddAlteracao(a, "Unidade", p.UnidadeId, unidade?.ToString(), unidade, ignorarVazio); return a.Where(x => x.Alterado).ToList(); }
    private static void AddAlteracao(List<AlteracaoProdutoImportacao> a, string f, object? atual, string? original, object? novo, bool ignorar) { if (ignorar && string.IsNullOrWhiteSpace(original)) return; var changed = !Equals(Convert.ToString(atual, CultureInfo.InvariantCulture), Convert.ToString(novo, CultureInfo.InvariantCulture)); a.Add(new(f, atual, original, novo, changed)); }
}
