using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;
using Orizon.Distribuidora.Web.Options;
using Orizon.Distribuidora.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
[Route("Admin/Importacao")]
public sealed class ImportacaoController : Controller
{
    private readonly IHistoricoImportacaoService historicoImportacaoService;
    private readonly ILeitorExcelService leitorExcelService;
    private readonly ICurrentCompanyAccessor currentCompanyAccessor;
    private readonly ImportacaoUploadValidator uploadValidator;
    private readonly ImportacaoOptions options;
    private readonly IMapeadorColunasService mapeadorColunasService;
    private readonly IModeloImportacaoService modeloImportacaoService;
    private readonly IValidadorDadosImportacaoService validadorDadosImportacaoService;
    private readonly IContextoValidacaoImportacaoService contextoValidacaoImportacaoService;
    private readonly IExecutorImportacaoProdutosService executorImportacaoProdutosService;
    private readonly IExportacaoImportacaoService exportacaoImportacaoService;

    public ImportacaoController(
        IHistoricoImportacaoService historicoImportacaoService,
        ILeitorExcelService leitorExcelService,
        ICurrentCompanyAccessor currentCompanyAccessor,
        ImportacaoUploadValidator uploadValidator,
        IOptions<ImportacaoOptions> options,
        IMapeadorColunasService mapeadorColunasService,
        IModeloImportacaoService modeloImportacaoService,
        IValidadorDadosImportacaoService validadorDadosImportacaoService,
        IContextoValidacaoImportacaoService contextoValidacaoImportacaoService,
        IExecutorImportacaoProdutosService executorImportacaoProdutosService,
        IExportacaoImportacaoService exportacaoImportacaoService)
    {
        this.historicoImportacaoService = historicoImportacaoService;
        this.leitorExcelService = leitorExcelService;
        this.currentCompanyAccessor = currentCompanyAccessor;
        this.uploadValidator = uploadValidator;
        this.options = options.Value;
        this.mapeadorColunasService = mapeadorColunasService;
        this.modeloImportacaoService = modeloImportacaoService;
        this.validadorDadosImportacaoService = validadorDadosImportacaoService;
        this.contextoValidacaoImportacaoService = contextoValidacaoImportacaoService;
        this.executorImportacaoProdutosService = executorImportacaoProdutosService;
        this.exportacaoImportacaoService = exportacaoImportacaoService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var historico = await historicoImportacaoService.ListarAsync(companyId);

        var model = new ImportacaoIndexViewModel
        {
            TotalImportacoes = historico.Count,
            ImportacoesComErro = historico.Count(item => item.Status == StatusImportacao.ProcessadaComErros),
            LinhasImportadas = historico.Sum(item => item.LinhasImportadas),
            UltimasImportacoes = historico.Take(10)
                .Select(item => new ImportacaoHistoricoResumoViewModel
                {
                    Id = item.Id,
                    NomeArquivo = item.NomeArquivo,
                    Status = item.Status,
                    TotalLinhas = item.TotalLinhas,
                    LinhasComErro = item.LinhasComErro,
                    LinhasImportadas = item.LinhasImportadas,
                    CriadoEm = item.CreatedAt
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet("Upload")]
    public IActionResult Upload()
    {
        return View(BuildUploadModel());
    }

    [HttpPost("Upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? arquivo, CancellationToken cancellationToken)
    {
        var uploadModel = BuildUploadModel();
        var validation = uploadValidator.Validate(arquivo);

        if (!validation.IsValid)
        {
            ModelState.AddModelError(nameof(arquivo), validation.ErrorMessage!);
            return View(uploadModel);
        }

        var fileName = validation.SanitizedFileName;
        var uploadedFile = arquivo!;
        var token = Guid.NewGuid().ToString("N");
        var temporaryPath = GetTemporaryPath(token);

        try
        {
            Directory.CreateDirectory(GetTemporaryDirectory());

            await using (var output = System.IO.File.Create(temporaryPath))
            {
                await uploadedFile.CopyToAsync(output, cancellationToken);
            }

            await using (var input = System.IO.File.OpenRead(temporaryPath))
            {
                var leitura = await leitorExcelService.LerAsync(
                    new ArquivoImportacaoExcel(input, fileName, uploadedFile.Length),
                    tamanhoAmostra: options.TamanhoAmostraPreview,
                    cancellationToken: cancellationToken);

                if (leitura.AbaAtual is null)
                {
                    throw new ImportacaoExcelException("O arquivo Excel não possui planilhas válidas.");
                }
            }

            var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
            var historico = await historicoImportacaoService.RegistrarAsync(
                companyId,
                new ArquivoImportacaoExcel(Stream.Null, fileName, uploadedFile.Length),
                GetCurrentUserId(),
                cancellationToken);

            return RedirectToAction(
                nameof(Preview),
                new
                {
                    id = historico.Id,
                    token
                });
        }
        catch (ImportacaoExcelException exception)
        {
            DeleteTemporaryFile(temporaryPath);
            ModelState.AddModelError(nameof(arquivo), exception.Message);
            return View(uploadModel);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            DeleteTemporaryFile(temporaryPath);
            ModelState.AddModelError(nameof(arquivo), "Não foi possível preparar o arquivo para leitura. Tente novamente.");
            return View(uploadModel);
        }
    }

    [HttpGet("Preview")]
    public async Task<IActionResult> Preview(
        Guid id,
        string token,
        string? aba,
        CancellationToken cancellationToken)
    {
        if (!IsValidToken(token))
        {
            TempData["Error"] = "Arquivo temporário inválido. Envie a planilha novamente.";
            return RedirectToAction(nameof(Upload));
        }

        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var historico = await historicoImportacaoService.ObterAsync(companyId, id, cancellationToken);

        if (historico is null)
        {
            return NotFound();
        }

        var temporaryPath = GetTemporaryPath(token);

        if (!System.IO.File.Exists(temporaryPath))
        {
            TempData["Error"] = "O arquivo temporário não está mais disponível. Envie a planilha novamente.";
            return RedirectToAction(nameof(Upload));
        }

        try
        {
            await using var input = System.IO.File.OpenRead(temporaryPath);
            var leitura = await leitorExcelService.LerAsync(
                new ArquivoImportacaoExcel(input, historico.NomeArquivo, historico.TamanhoArquivoBytes),
                aba,
                options.TamanhoAmostraPreview,
                cancellationToken);

            return View(BuildPreviewModel(historico.Id, token, historico.NomeArquivo, historico.TamanhoArquivoBytes, leitura));
        }
        catch (ImportacaoExcelException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Upload));
        }
    }

    [HttpPost("Executar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Executar(Guid id,CancellationToken cancellationToken)
    {try{var companyId=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);await executorImportacaoProdutosService.ExecutarAsync(id,companyId,GetCurrentUserId(),cancellationToken);return RedirectToAction(nameof(Resultado),new{id});}catch(ImportacaoExecucaoException ex){TempData["Error"]=ex.Message;return RedirectToAction(nameof(Resultado),new{id});}}

    [HttpGet("Resultado")]
    public async Task<IActionResult> Resultado(Guid id,string? filtro,string? busca,int pagina=1,CancellationToken cancellationToken=default)
    {var companyId=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var result=await executorImportacaoProdutosService.ObterResultadoAsync(id,companyId,cancellationToken);if(result is null)return NotFound();IEnumerable<ResultadoExecucaoItem> query=result.Itens;query=filtro switch{"inseridos"=>query.Where(x=>x.Status==StatusLinhaImportacao.Inserida),"atualizados"=>query.Where(x=>x.Status==StatusLinhaImportacao.Atualizada),"semAlteracao"=>query.Where(x=>x.Status==StatusLinhaImportacao.SemAlteracao),"ignorados"=>query.Where(x=>x.Status==StatusLinhaImportacao.Ignorada),"bloqueados"=>query.Where(x=>x.Status==StatusLinhaImportacao.Bloqueada),"falhas"=>query.Where(x=>x.Status==StatusLinhaImportacao.Falhou),_=>query};if(!string.IsNullOrWhiteSpace(busca))query=query.Where(x=>x.Linha.ToString()==busca||(x.Codigo?.Contains(busca,StringComparison.OrdinalIgnoreCase)??false)||(x.Descricao?.Contains(busca,StringComparison.OrdinalIgnoreCase)??false)||(x.Mensagem?.Contains(busca,StringComparison.OrdinalIgnoreCase)??false));const int size=50;var total=query.Count();var pages=Math.Max(1,(int)Math.Ceiling(total/(double)size));pagina=Math.Clamp(pagina,1,pages);return View(new ImportacaoResultadoViewModel{Resultado=result,Itens=query.Skip((pagina-1)*size).Take(size).ToList(),Pagina=pagina,TotalPaginas=pages,Filtro=filtro,Busca=busca});}

    [HttpGet("ExportarExcel")]
    public async Task<IActionResult> ExportarExcel(Guid id,FiltroExportacaoImportacao filtro=FiltroExportacaoImportacao.Todos,CancellationToken cancellationToken=default)
    {try{var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var file=await exportacaoImportacaoService.ExportarExcelAsync(id,company,GetCurrentUserId(),filtro,cancellationToken);return File(file.Conteudo,file.ContentType,file.NomeArquivo);}catch(Exception ex) when(ex is KeyNotFoundException or UnauthorizedAccessException){return NotFound();}}

    [HttpGet("ExportarCsv")]
    public async Task<IActionResult> ExportarCsv(Guid id,FiltroExportacaoImportacao filtro=FiltroExportacaoImportacao.Todos,CancellationToken cancellationToken=default)
    {try{var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var file=await exportacaoImportacaoService.ExportarCsvAsync(id,company,GetCurrentUserId(),filtro,cancellationToken);return File(file.Conteudo,file.ContentType,file.NomeArquivo);}catch(Exception ex) when(ex is KeyNotFoundException or UnauthorizedAccessException){return NotFound();}}

    [HttpGet("Mapeamento")]
    public async Task<IActionResult> Mapeamento(Guid id, string token, string? aba, Guid? modeloId, CancellationToken cancellationToken)
    {
        if (!IsValidToken(token)) return RedirectToAction(nameof(Upload));
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var historico = await historicoImportacaoService.ObterAsync(companyId, id, cancellationToken);
        var path = GetTemporaryPath(token);
        if (historico is null || !System.IO.File.Exists(path)) return RedirectToAction(nameof(Upload));
        await using var input = System.IO.File.OpenRead(path);
        var leitura = await leitorExcelService.LerAsync(new(input, historico.NomeArquivo, historico.TamanhoArquivoBytes), aba, 20, cancellationToken);
        var planilha = leitura.AbaAtual!;
        var modelos = await modeloImportacaoService.ListarAsync(companyId, GetCurrentUserId(), cancellationToken);
        var modelo = modeloId.HasValue ? modelos.FirstOrDefault(x => x.Id == modeloId) : await modeloImportacaoService.EncontrarCompativelAsync(companyId, GetCurrentUserId(), planilha.Cabecalhos, cancellationToken);
        var automatico = modelo is null ? await mapeadorColunasService.MapearAsync(planilha.Cabecalhos, cancellationToken) : new MapeamentoColunasImportacao(modelo.Mapeamentos);
        return View(new ImportacaoMapeamentoViewModel { ImportacaoId=id, TokenArquivo=token, NomeArquivo=historico.NomeArquivo, AbaSelecionada=leitura.AbaSelecionada, Cabecalhos=planilha.Cabecalhos, Amostra=planilha.Amostra.Select(x => new ImportacaoLinhaAmostraViewModel { NumeroLinha=x.NumeroLinha, Valores=x.Valores }).ToList(), Campos=CatalogoCamposProdutoImportacao.Campos, Mapeamentos=automatico.Colunas, Confiancas=automatico.Confiancas ?? new Dictionary<string,double>(), Modelos=modelos, ModeloCarregadoId=modelo?.Id });
    }

    [HttpPost("SalvarModelo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarModelo([FromBody] SalvarModeloImportacaoRequest request, CancellationToken cancellationToken)
    { var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User); var model = await modeloImportacaoService.SalvarAsync(companyId, GetCurrentUserId(), request.Nome, request.Padrao, request.Mapeamentos, request.Cabecalhos, cancellationToken); return Json(new { model.Id, model.Nome }); }

    [HttpPost("ExcluirModelo/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirModelo(Guid id, CancellationToken cancellationToken)
    { var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User); await modeloImportacaoService.ExcluirAsync(companyId, id, cancellationToken); return NoContent(); }

    [HttpPost("ValidarMapeamento")]
    [ValidateAntiForgeryToken]
    public IActionResult ValidarMapeamento([FromBody] SalvarModeloImportacaoRequest request) => Json(ValidadorMapeamentoColunas.Validar(request.Mapeamentos, request.Cabecalhos, request.Amostra));

    [HttpPost("Validacao")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validacao(ExecutarValidacaoImportacaoRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidToken(request.TokenArquivo)) return RedirectToAction(nameof(Upload));
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var historico = await historicoImportacaoService.ObterAsync(companyId, request.ImportacaoId, cancellationToken);
        var path = GetTemporaryPath(request.TokenArquivo);
        if (historico is null || !System.IO.File.Exists(path)) return RedirectToAction(nameof(Upload));
        Dictionary<string,string>? mappings; try { mappings=JsonSerializer.Deserialize<Dictionary<string,string>>(request.MapeamentoJson); } catch(JsonException) { return BadRequest(); }
        if(mappings is null) return BadRequest();
        await using var input=System.IO.File.OpenRead(path);
        var planilha=await leitorExcelService.LerAsync(new(input,historico.NomeArquivo,historico.TamanhoArquivoBytes),request.AbaSelecionada,10_000,cancellationToken);
        var aba=planilha.AbaAtual!; var mapValidation=ValidadorMapeamentoColunas.Validar(mappings,aba.Cabecalhos); if(!mapValidation.Valido)return BadRequest("Mapeamento inválido.");
        var options=new OpcoesValidacaoImportacao(request.InserirNovos,request.AtualizarExistentes,request.IgnorarVaziosAtualizacao,request.PermitirImportacaoParcial,request.BloquearComQualquerErro,true);
        var context=await contextoValidacaoImportacaoService.PrepararAsync(request.ImportacaoId,companyId,GetCurrentUserId(),aba.Amostra,new(mappings),options,cancellationToken);
        var result=await validadorDadosImportacaoService.ValidarAsync(context,cancellationToken);
        if(request.PersistirResultado) await historicoImportacaoService.SalvarValidacaoAsync(companyId,request.ImportacaoId,GetCurrentUserId(),result,options,cancellationToken);
        IEnumerable<ResultadoValidacaoLinha> query=result.Linhas;
        query=request.Filtro switch { "validas"=>query.Where(x=>x.PodeImportar),"novas"=>query.Where(x=>x.Operacao==TipoOperacaoImportacao.Inserir),"atualizacoes"=>query.Where(x=>x.Operacao==TipoOperacaoImportacao.Atualizar),"avisos"=>query.Where(x=>x.Avisos.Count>0),"erros"=>query.Where(x=>x.Erros.Count>0),"duplicadas"=>query.Where(x=>x.Status==StatusValidacaoLinha.Duplicada),"ignoradas"=>query.Where(x=>x.Status==StatusValidacaoLinha.Ignorada),_=>query};
        if(!string.IsNullOrWhiteSpace(request.Busca)){var term=request.Busca.Trim();query=query.Where(x=>x.NumeroLinha.ToString()==term||(x.CodigoProduto?.Contains(term,StringComparison.OrdinalIgnoreCase)??false)||(x.Descricao?.Contains(term,StringComparison.OrdinalIgnoreCase)??false)||x.Erros.Concat(x.Avisos).Any(e=>e.Mensagem.Contains(term,StringComparison.OrdinalIgnoreCase)));}
        const int pageSize=50; var total=query.Count();var pages=Math.Max(1,(int)Math.Ceiling(total/(double)pageSize));request.Pagina=Math.Clamp(request.Pagina,1,pages);
        return View(new ImportacaoValidacaoViewModel{ImportacaoId=request.ImportacaoId,NomeArquivo=historico.NomeArquivo,AbaSelecionada=aba.Nome,Resultado=result,Linhas=query.Skip((request.Pagina-1)*pageSize).Take(pageSize).ToList(),Pagina=request.Pagina,TotalPaginas=pages,Filtro=request.Filtro,Busca=request.Busca,Request=request});
    }

    [HttpGet("Historico")]
    public async Task<IActionResult> Historico([FromQuery] ConsultaHistoricoImportacao consulta,CancellationToken cancellationToken)
    {var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var page=await historicoImportacaoService.ConsultarAsync(company,consulta,cancellationToken);return View(new HistoricoImportacaoViewModel{Consulta=consulta,Pagina=page});}

    [HttpGet("Detalhes/{id:guid}")]
    public async Task<IActionResult> Detalhes(Guid id,CancellationToken cancellationToken)
    {var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var details=await historicoImportacaoService.ObterDetalhesAsync(company,id,cancellationToken);if(details is null)return NotFound();return View(new HistoricoImportacaoDetalhesViewModel{Detalhes=details});}

    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard(DateTimeOffset? inicio,DateTimeOffset? fim,CancellationToken cancellationToken)
    {var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);return Json(await historicoImportacaoService.ObterDashboardAsync(company,inicio,fim,cancellationToken));}

    [HttpPost("Duplicar/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicar(Guid id,CancellationToken cancellationToken)
    {try{var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);await historicoImportacaoService.DuplicarConfiguracaoAsync(company,id,GetCurrentUserId(),cancellationToken);TempData["Success"]="Configuração duplicada. Selecione o novo modelo no próximo mapeamento.";return RedirectToAction(nameof(Upload));}catch(KeyNotFoundException){return NotFound();}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;return RedirectToAction(nameof(Historico));}}

    [HttpPost("Excluir/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(Guid id,CancellationToken cancellationToken)
    {try{var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);await historicoImportacaoService.ExcluirHistoricoAsync(company,id,GetCurrentUserId(),cancellationToken);TempData["Success"]="Histórico excluído com auditoria.";return RedirectToAction(nameof(Historico));}catch(KeyNotFoundException){return NotFound();}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;return RedirectToAction(nameof(Historico));}}

    private ImportacaoUploadViewModel BuildUploadModel()
    {
        return new ImportacaoUploadViewModel
        {
            TamanhoMaximoArquivoMB = options.TamanhoMaximoArquivoMB,
            FormatosAceitos = string.Join(", ", options.GetExtensoesPermitidas())
        };
    }

    private static ImportacaoPreviewViewModel BuildPreviewModel(
        Guid importacaoId,
        string token,
        string nomeArquivo,
        long tamanhoArquivoBytes,
        PlanilhaImportada leitura)
    {
        var abaAtual = leitura.AbaAtual;

        return new ImportacaoPreviewViewModel
        {
            ImportacaoId = importacaoId,
            TokenArquivo = token,
            NomeArquivo = nomeArquivo,
            TamanhoArquivoBytes = tamanhoArquivoBytes,
            AbaSelecionada = leitura.AbaSelecionada,
            Abas = leitura.Abas
                .Select(aba => new ImportacaoAbaViewModel
                {
                    Nome = aba.Nome,
                    QuantidadeLinhas = aba.QuantidadeLinhas,
                    QuantidadeColunas = aba.Cabecalhos.Count
                })
                .ToList(),
            Cabecalhos = abaAtual?.Cabecalhos ?? [],
            QuantidadeLinhas = abaAtual?.QuantidadeLinhas ?? 0,
            Amostra = abaAtual?.Amostra
                .Select(linha => new ImportacaoLinhaAmostraViewModel
                {
                    NumeroLinha = linha.NumeroLinha,
                    Valores = linha.Valores
                })
                .ToList() ?? []
        };
    }

    private static bool IsValidToken(string token) =>
        Guid.TryParseExact(token, "N", out _);

    private static string GetTemporaryDirectory() =>
        Path.Combine(Path.GetTempPath(), "orizon-importacoes");

    private static string GetTemporaryPath(string token) =>
        Path.Combine(GetTemporaryDirectory(), $"{token}.xlsx");

    private static void DeleteTemporaryFile(string path)
    {
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }
}
