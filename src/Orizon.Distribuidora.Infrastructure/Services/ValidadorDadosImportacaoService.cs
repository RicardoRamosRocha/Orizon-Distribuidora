using System.Globalization;
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
        var codigos = uteis.GroupBy(x => NormalizarCodigo(Valor(x, contexto.Mapeamento, "codigo")), StringComparer.Ordinal).Where(x => x.Key.Length > 0 && x.Count() > 1).ToDictionary(x => x.Key, x => x.Select(l => l.NumeroLinha).ToArray());
        var barras = uteis.GroupBy(x => NormalizarBarras(Valor(x, contexto.Mapeamento, "codigoBarras")), StringComparer.Ordinal).Where(x => x.Key.Length > 0 && x.Count() > 1).ToDictionary(x => x.Key, x => x.Select(l => l.NumeroLinha).ToArray());
        var resultados = new List<ResultadoValidacaoLinha>(uteis.Count);

        foreach (var linha in uteis)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var erros = new List<ErroValidacaoImportacao>(); var avisos = new List<ErroValidacaoImportacao>(); var convertidos = new Dictionary<string, object?>();
            var codigo = NormalizarCodigo(Valor(linha, contexto.Mapeamento, "codigo")); var descricao = Valor(linha, contexto.Mapeamento, "descricao")?.Trim(); var barcode = NormalizarBarras(Valor(linha, contexto.Mapeamento, "codigoBarras"));
            Obrigatorio(codigo, "codigo", "Código", linha, erros); Obrigatorio(descricao, "descricao", "Descrição", linha, erros);
            if (codigo.Length > 50) Erro(linha, erros, "codigo", codigo, "IMP_VALOR_FORA_LIMITE", "O código deve possuir no máximo 50 caracteres.");
            if ((descricao?.Length ?? 0) > 200) Erro(linha, erros, "descricao", descricao, "IMP_VALOR_FORA_LIMITE", "A descrição deve possuir no máximo 200 caracteres.");
            if (barcode.Length > 32 || (barcode.Length > 0 && barcode.Any(x => !char.IsDigit(x)))) Erro(linha, erros, "codigoBarras", barcode, "IMP_CODIGO_BARRAS_INVALIDO", "O código de barras deve conter até 32 dígitos.");
            if (codigos.TryGetValue(codigo, out var linhasCodigo)) Erro(linha, erros, "codigo", codigo, "IMP_DUPLICIDADE_CODIGO_PLANILHA", $"Código duplicado nas linhas {string.Join(", ", linhasCodigo)}.");
            if (barcode.Length > 0 && barras.TryGetValue(barcode, out var linhasBarras)) Erro(linha, erros, "codigoBarras", barcode, "IMP_DUPLICIDADE_BARRAS_PLANILHA", $"Código de barras duplicado nas linhas {string.Join(", ", linhasBarras)}.");
            convertidos["codigo"] = codigo; convertidos["descricao"] = descricao; if (barcode.Length > 0) convertidos["codigoBarras"] = barcode;
            var custo = Decimal(linha, contexto.Mapeamento, "precoCompra", erros); var venda = Decimal(linha, contexto.Mapeamento, "precoVenda", erros); var estoque = Decimal(linha, contexto.Mapeamento, "estoqueInicial", erros); var peso = Decimal(linha, contexto.Mapeamento, "peso", erros);
            AddDecimal(convertidos,"precoCompra",custo); AddDecimal(convertidos,"precoVenda",venda); AddDecimal(convertidos,"estoqueInicial",estoque); AddDecimal(convertidos,"peso",peso);
            if (custo < 0) Erro(linha, erros,"precoCompra",custo?.ToString(),"IMP_VALOR_NEGATIVO","O preço de custo não pode ser negativo.");
            if (venda < 0) Erro(linha, erros,"precoVenda",venda?.ToString(),"IMP_VALOR_NEGATIVO","O preço de venda não pode ser negativo.");
            if (estoque < 0) Erro(linha, erros,"estoqueInicial",estoque?.ToString(),"IMP_VALOR_NEGATIVO","O estoque não pode ser negativo.");
            if (venda == 0) Aviso(linha, avisos,"precoVenda",venda?.ToString(),"IMP_PRECO_ZERADO","O preço de venda está zerado.");
            if (venda.HasValue && custo.HasValue && venda < custo) Aviso(linha, avisos,"precoVenda",venda.ToString(),"IMP_PRECO_MENOR_CUSTO","O preço de venda é menor que o custo.");
            var tipo = TipoProduto(Valor(linha, contexto.Mapeamento,"tipoProduto")); if (tipo is not null) convertidos["tipoProduto"] = tipo;
            if (tipo == "Terceiro" && estoque is not null) Erro(linha, erros,"estoqueInicial",estoque.ToString(),"IMP_TERCEIRO_COM_ESTOQUE","Produto de terceiro não pode receber estoque físico.");
            var unidade = Valor(linha, contexto.Mapeamento,"unidade")?.Trim(); if (!string.IsNullOrWhiteSpace(unidade)) { var key=NormalizarTexto(unidade); if(contexto.Unidades.TryGetValue(key,out var unitId)) convertidos["unidadeId"]=unitId; else Erro(linha,erros,"unidade",unidade,"IMP_CADASTRO_INEXISTENTE","Unidade de medida não encontrada para esta empresa."); }
            produtosCodigo.TryGetValue(codigo, out var existente); if (existente is null && barcode.Length > 0 && produtosBarras.TryGetValue(barcode,out var conflito)) Erro(linha,erros,"codigoBarras",barcode,"IMP_CODIGO_BARRAS_EXISTENTE",$"Código de barras já pertence ao produto {conflito.Codigo}.");
            var alteracoes = existente is null ? [] : Comparar(existente, descricao, barcode, custo, venda, convertidos.TryGetValue("unidadeId",out var uid)?uid:null, contexto.Opcoes.IgnorarVaziosAtualizacao);
            var operacao = erros.Count > 0 ? TipoOperacaoImportacao.Bloquear : existente is null ? (contexto.Opcoes.InserirNovos ? TipoOperacaoImportacao.Inserir : TipoOperacaoImportacao.Ignorar) : alteracoes.Count > 0 && contexto.Opcoes.AtualizarExistentes ? TipoOperacaoImportacao.Atualizar : TipoOperacaoImportacao.Ignorar;
            var status = erros.Any(x=>x.Codigo.StartsWith("IMP_DUPLICIDADE")) ? StatusValidacaoLinha.Duplicada : erros.Count>0 ? StatusValidacaoLinha.Invalida : avisos.Count>0 ? StatusValidacaoLinha.ComAviso : operacao==TipoOperacaoImportacao.Ignorar ? StatusValidacaoLinha.Ignorada : StatusValidacaoLinha.Valida;
            resultados.Add(new(linha.NumeroLinha,status,codigo,descricao,convertidos,linha.Valores,operacao,existente,erros,avisos,alteracoes,erros.Count==0&&operacao is TipoOperacaoImportacao.Inserir or TipoOperacaoImportacao.Atualizar,operacao==TipoOperacaoImportacao.Atualizar));
        }
        var ignored = contexto.Linhas.Count - uteis.Count; var invalid = resultados.Count(x=>!x.PodeImportar&&x.Status is StatusValidacaoLinha.Invalida or StatusValidacaoLinha.Duplicada); var valid=resultados.Count(x=>x.PodeImportar);
        var canImport=valid>0 && (contexto.Opcoes.PermitirImportacaoParcial || invalid==0) && (!contexto.Opcoes.BloquearComQualquerErro || invalid==0);
        return Task.FromResult(new ResultadoValidacaoImportacao(uteis.Count,valid,invalid,resultados.Count(x=>x.Avisos.Count>0),resultados.Count(x=>x.Operacao==TipoOperacaoImportacao.Inserir),resultados.Count(x=>x.ProdutoExistente is not null),resultados.Count(x=>x.Operacao==TipoOperacaoImportacao.Atualizar),resultados.Count(x=>x.Status==StatusValidacaoLinha.Duplicada),ignored,canImport,resultados,DateTimeOffset.UtcNow));
    }

    public static string NormalizarCodigo(string? value)=>string.IsNullOrWhiteSpace(value)?string.Empty:value.Trim().ToUpperInvariant();
    public static string NormalizarTexto(string value)=>MapeadorColunasService.Normalizar(value);
    private static string NormalizarBarras(string? value)=>string.IsNullOrWhiteSpace(value)?string.Empty:value.Trim();
    private static string? Valor(LinhaPlanilhaImportada l,MapeamentoColunasImportacao m,string campo)=>m.Colunas.TryGetValue(campo,out var c)&&l.Valores.TryGetValue(c,out var v)?v:null;
    private static void Obrigatorio(string? v,string campo,string nome,LinhaPlanilhaImportada l,List<ErroValidacaoImportacao> e){if(string.IsNullOrWhiteSpace(v))Erro(l,e,campo,v,"IMP_CAMPO_OBRIGATORIO",$"{nome} é obrigatório.");}
    private static decimal? Decimal(LinhaPlanilhaImportada l,MapeamentoColunasImportacao m,string campo,List<ErroValidacaoImportacao> e){var v=Valor(l,m,campo);if(string.IsNullOrWhiteSpace(v))return null;if(TryDecimal(v,out var d))return d;Erro(l,e,campo,v,"IMP_NUMERO_INVALIDO",$"'{v}' não é um número válido.");return null;}
    public static bool TryDecimal(string value,out decimal result){value=value.Trim();var culture=value.Contains(',')?new CultureInfo("pt-BR"):CultureInfo.InvariantCulture;return decimal.TryParse(value,NumberStyles.Number,culture,out result);}
    private static string? TipoProduto(string? v){if(string.IsNullOrWhiteSpace(v))return null;var n=NormalizarTexto(v);return n is "terceiro" or "thirdparty"?"Terceiro":n is "proprio" or "own"?"Próprio":null;}
    private static void AddDecimal(Dictionary<string,object?> d,string k,decimal? v){if(v.HasValue)d[k]=v.Value;}
    private static void Erro(LinhaPlanilhaImportada l,List<ErroValidacaoImportacao> e,string c,string? v,string code,string m)=>e.Add(new(l.NumeroLinha,c,Limitar(v),code,m,SeveridadeValidacao.Erro,DateTimeOffset.UtcNow));
    private static void Aviso(LinhaPlanilhaImportada l,List<ErroValidacaoImportacao> e,string c,string? v,string code,string m)=>e.Add(new(l.NumeroLinha,c,Limitar(v),code,m,SeveridadeValidacao.Aviso,DateTimeOffset.UtcNow));
    private static string? Limitar(string? v)=>v?.Length>500?v[..500]:v;
    private static List<AlteracaoProdutoImportacao> Comparar(ProdutoExistenteImportacao p,string? nome,string barcode,decimal? custo,decimal? venda,object? unidade,bool ignorarVazio){var a=new List<AlteracaoProdutoImportacao>();Add(a,"Descrição",p.Descricao,nome,nome,ignorarVazio);Add(a,"Código de barras",p.CodigoBarras,barcode,barcode,ignorarVazio);Add(a,"Preço Compra",p.PrecoCusto,custo?.ToString(),custo,ignorarVazio);Add(a,"Preço Venda",p.PrecoVenda,venda?.ToString(),venda,ignorarVazio);Add(a,"Unidade",p.UnidadeId,unidade?.ToString(),unidade,ignorarVazio);return a.Where(x=>x.Alterado).ToList();}
    private static void Add(List<AlteracaoProdutoImportacao> a,string f,object? atual,string? original,object? novo,bool ignorar){if(ignorar&&string.IsNullOrWhiteSpace(original))return;var changed=!Equals(Convert.ToString(atual,CultureInfo.InvariantCulture),Convert.ToString(novo,CultureInfo.InvariantCulture));a.Add(new(f,atual,original,novo,changed));}
}
