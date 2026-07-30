using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Application.Stock;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;
using Orizon.Distribuidora.Web.Options;
using Orizon.Distribuidora.Web.Services;
using System.Security.Claims;

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
    private readonly IRollbackImportacaoService rollbackImportacaoService;
    private readonly ImportacaoArquivoTemporarioService arquivoTemporarioService;
    private readonly IStockService stockService;

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
        IExportacaoImportacaoService exportacaoImportacaoService,
        IRollbackImportacaoService rollbackImportacaoService,
        ImportacaoArquivoTemporarioService arquivoTemporarioService,
        IStockService stockService)
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
        this.rollbackImportacaoService = rollbackImportacaoService;
        this.arquivoTemporarioService = arquivoTemporarioService;
        this.stockService = stockService;
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
        try
        {
            await using (var input = uploadedFile.OpenReadStream())
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
            var temporary = await arquivoTemporarioService.SalvarAsync(uploadedFile, historico.Id, companyId, GetCurrentUserId(), cancellationToken);

            return RedirectToAction(
                nameof(Preview),
                new
                {
                    id = historico.Id,
                    token = temporary.Token
                });
        }
        catch (ImportacaoExcelException exception)
        {
            ModelState.AddModelError(nameof(arquivo), exception.Message);
            return View(uploadModel);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
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

        var temporary = await arquivoTemporarioService.ObterAsync(token, id, companyId, GetCurrentUserId(), cancellationToken);
        if (temporary is null)
        {
            TempData["Error"] = "O arquivo temporário não está mais disponível. Envie a planilha novamente.";
            return RedirectToAction(nameof(Upload));
        }

        try
        {
            await using var input = System.IO.File.OpenRead(temporary.Caminho);
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
    public async Task<IActionResult> Executar(Guid id, string? token, CancellationToken cancellationToken)
    {
        try
        {
            var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
            await executorImportacaoProdutosService.ExecutarAsync(id, companyId, GetCurrentUserId(), cancellationToken);
            if (!string.IsNullOrWhiteSpace(token) &&
                await arquivoTemporarioService.ObterAsync(token, id, companyId, GetCurrentUserId(), cancellationToken) is not null)
            {
                arquivoTemporarioService.Excluir(token);
            }
            return RedirectToAction(nameof(Resultado), new { id });
        }
        catch (ImportacaoExecucaoException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Resultado), new { id });
        }
    }

    [HttpPost("Cancelar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(Guid id, string token, CancellationToken cancellationToken)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var temporary = await arquivoTemporarioService.ObterAsync(token, id, companyId, GetCurrentUserId(), cancellationToken);
        if (temporary is null) return RedirectToAction(nameof(Upload));
        await historicoImportacaoService.CancelarAsync(companyId, id, GetCurrentUserId(), cancellationToken);
        arquivoTemporarioService.Excluir(token);
        TempData["Success"] = "ImportaÃ§Ã£o cancelada e arquivo temporÃ¡rio removido.";
        return RedirectToAction(nameof(Historico));
    }

    [HttpGet("Resultado")]
    public async Task<IActionResult> Resultado(Guid id,string? filtro,string? busca,int pagina=1,CancellationToken cancellationToken=default)
    {var companyId=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var page=await executorImportacaoProdutosService.ObterResultadoPaginaAsync(id,companyId,filtro,busca,pagina,50,cancellationToken);if(page is null)return NotFound();return View(new ImportacaoResultadoViewModel{Resultado=page.Resultado,Itens=page.Itens,Pagina=page.Pagina,TotalPaginas=page.TotalPaginas,Filtro=filtro,Busca=busca});}

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
        var temporary = await arquivoTemporarioService.ObterAsync(token, id, companyId, GetCurrentUserId(), cancellationToken);
        if (historico is null || temporary is null) return RedirectToAction(nameof(Upload));
        await using var input = System.IO.File.OpenRead(temporary.Caminho);
        var leitura = await leitorExcelService.LerAsync(new(input, historico.NomeArquivo, historico.TamanhoArquivoBytes), aba, 20, cancellationToken);
        var planilha = leitura.AbaAtual!;
        var modelos = await modeloImportacaoService.ListarAsync(companyId, GetCurrentUserId(), cancellationToken);
        var modelo = modeloId.HasValue
            ? modelos.FirstOrDefault(x => x.Id == modeloId)
            : temporary.Mapeamentos is { Count: > 0 }
                ? null
                : await modeloImportacaoService.EncontrarCompativelAsync(companyId, GetCurrentUserId(), planilha.Cabecalhos, cancellationToken);
        var automatico = modelo is not null
            ? new MapeamentoColunasImportacao(modelo.Mapeamentos)
            : temporary.Mapeamentos is { Count: > 0 }
                ? new MapeamentoColunasImportacao(temporary.Mapeamentos)
                : await mapeadorColunasService.MapearAsync(planilha.Cabecalhos, cancellationToken);
        var stockOptions = await stockService.GetWorkspaceOptionsAsync(companyId, cancellationToken);
        return View(new ImportacaoMapeamentoViewModel
        {
            ImportacaoId = id,
            TokenArquivo = token,
            NomeArquivo = historico.NomeArquivo,
            AbaSelecionada = leitura.AbaSelecionada,
            Cabecalhos = planilha.Cabecalhos,
            Amostra = planilha.Amostra.Select(x => new ImportacaoLinhaAmostraViewModel { NumeroLinha=x.NumeroLinha, Valores=x.Valores }).ToList(),
            Campos = CatalogoCamposProdutoImportacao.Campos,
            Mapeamentos = automatico.Colunas,
            Confiancas = automatico.Confiancas ?? new Dictionary<string,double>(),
            Conflitos = automatico.Conflitos ?? new Dictionary<string, IReadOnlyList<string>>(),
            ErrosMapeamento = TempData["MapeamentoErro"] is string mensagem ? [new(string.Empty, mensagem)] : [],
            Modelos = modelos,
            ModeloCarregadoId = modelo?.Id,
            Depositos = stockOptions.Warehouses,
            LocaisInternos = stockOptions.Locations
        });
    }

    [HttpPost("SalvarModelo")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarModelo([FromBody] SalvarModeloImportacaoRequest request, CancellationToken cancellationToken)
    { try { var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User); var model = await modeloImportacaoService.SalvarAsync(companyId, GetCurrentUserId(), request.Nome, request.Padrao, request.Mapeamentos, request.Cabecalhos, cancellationToken); return Json(new { model.Id, model.Nome }); } catch (ArgumentException ex) { return BadRequest(ex.Message); } }

    [HttpPost("ExcluirModelo/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirModelo(Guid id, CancellationToken cancellationToken)
    { try { var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User); await modeloImportacaoService.ExcluirAsync(companyId, id, GetCurrentUserId(), cancellationToken); return NoContent(); } catch (UnauthorizedAccessException) { return Forbid(); } }

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
        var temporary = await arquivoTemporarioService.ObterAsync(request.TokenArquivo, request.ImportacaoId, companyId, GetCurrentUserId(), cancellationToken);
        if (historico is null || temporary is null) return RedirectToAction(nameof(Upload));
        var mappings = MapeamentoColunasImportacao.Canonicalizar(request.Mapeamentos);
        await using var input=System.IO.File.OpenRead(temporary.Caminho);
        var planilha=await leitorExcelService.LerAsync(new(input,historico.NomeArquivo,historico.TamanhoArquivoBytes),request.AbaSelecionada,10_000,cancellationToken);
        var aba = planilha.AbaAtual!;
        var mapValidation = ValidadorMapeamentoColunas.Validar(mappings, aba.Cabecalhos, aba.Amostra.Select(x => x.Valores).ToList());
        await arquivoTemporarioService.SalvarMapeamentoAsync(
            request.TokenArquivo,
            request.ImportacaoId,
            companyId,
            GetCurrentUserId(),
            mappings,
            aba.Nome,
            cancellationToken);
        if (!mapValidation.Valido)
        {
            TempData["MapeamentoErro"] = string.Join(" ", mapValidation.Erros.Select(x => x.Mensagem));
            return RedirectToAction(nameof(Mapeamento), new
            {
                id = request.ImportacaoId,
                token = request.TokenArquivo,
                aba = aba.Nome
            });
        }
        var options = new OpcoesValidacaoImportacao(
            request.InserirNovos,
            request.AtualizarExistentes,
            request.IgnorarVaziosAtualizacao,
            request.PermitirImportacaoParcial,
            request.BloquearComQualquerErro,
            true,
            request.DepositoId,
            request.LocalInternoId,
            mappings,
            aba.Nome);
        var context=await contextoValidacaoImportacaoService.PrepararAsync(request.ImportacaoId,companyId,GetCurrentUserId(),aba.Amostra,new(mappings),options,cancellationToken);
        var result=await validadorDadosImportacaoService.ValidarAsync(context,cancellationToken);
        if(request.PersistirResultado)
        {
            if(request.ModeloImportacaoId.HasValue) await historicoImportacaoService.AssociarModeloAsync(companyId,request.ImportacaoId,request.ModeloImportacaoId.Value,GetCurrentUserId(),cancellationToken);
            await historicoImportacaoService.SalvarValidacaoAsync(companyId,request.ImportacaoId,GetCurrentUserId(),result,options,cancellationToken);
        }
        return RedirectToAction(nameof(ValidacaoResultado),new{id=request.ImportacaoId,token=request.TokenArquivo});
    }

    [HttpGet("Validacao/{id:guid}")]
    public async Task<IActionResult> ValidacaoResultado(Guid id,string? token,string? filtro,string? busca,int pagina=1,CancellationToken cancellationToken=default)
    {
        var companyId=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var history=await historicoImportacaoService.ObterAsync(companyId,id,cancellationToken);
        var page=await historicoImportacaoService.ObterValidacaoAsync(companyId,id,filtro,busca,pagina,50,cancellationToken);if(history is null||page is null)return NotFound();
        var options = string.IsNullOrWhiteSpace(history.OpcoesValidacaoJson)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<OpcoesValidacaoImportacao>(history.OpcoesValidacaoJson);
        return View("Validacao",new ImportacaoValidacaoViewModel
        {
            ImportacaoId=id,
            NomeArquivo=history.NomeArquivo,
            TokenArquivo=token,
            AbaSelecionada=options?.AbaSelecionada,
            Resultado=page.Resultado,
            Linhas=page.Linhas,
            Pagina=page.Pagina,
            TotalPaginas=page.TotalPaginas,
            Filtro=filtro,
            Busca=busca,
            Request=new(){ImportacaoId=id},
            Mapeamentos=options?.Mapeamentos ?? new Dictionary<string,string>()
        });
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

    [HttpGet("Rollback/{id:guid}")]
    public async Task<IActionResult> Rollback(Guid id,CancellationToken cancellationToken)
    {try{var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);return View(new RollbackConfirmacaoViewModel{Analise=await rollbackImportacaoService.AnalisarAsync(id,company,GetCurrentUserId(),cancellationToken)});}catch(UnauthorizedAccessException){return NotFound();}catch(RollbackImportacaoException ex){TempData["Error"]=ex.Message;return RedirectToAction(nameof(Historico));}}

    [HttpPost("ExecutarRollback/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecutarRollback(Guid id,string confirmacao,bool permitirParcial,CancellationToken cancellationToken)
    {if(!string.Equals(confirmacao,"CONFIRMAR",StringComparison.Ordinal)) {TempData["Error"]="Digite CONFIRMAR para autorizar o rollback.";return RedirectToAction(nameof(Rollback),new{id});}try{var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);await rollbackImportacaoService.ExecutarAsync(id,company,GetCurrentUserId(),permitirParcial,cancellationToken);return RedirectToAction(nameof(RollbackResultado),new{id});}catch(UnauthorizedAccessException){return NotFound();}catch(RollbackImportacaoException ex){TempData["Error"]=ex.Message;return RedirectToAction(nameof(Rollback),new{id});}}

    [HttpGet("RollbackResultado/{id:guid}")]
    public async Task<IActionResult> RollbackResultado(Guid id,CancellationToken cancellationToken)
    {var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var result=await rollbackImportacaoService.ObterResultadoAsync(id,company,cancellationToken);return result is null?NotFound():View(new RollbackResultadoViewModel{Resultado=result});}
    [HttpGet("RollbackExcel/{id:guid}")]
    public async Task<IActionResult> RollbackExcel(Guid id,CancellationToken cancellationToken){var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var f=await rollbackImportacaoService.ExportarExcelAsync(id,company,cancellationToken);return File(f.Conteudo,f.ContentType,f.NomeArquivo);}
    [HttpGet("RollbackCsv/{id:guid}")]
    public async Task<IActionResult> RollbackCsv(Guid id,CancellationToken cancellationToken){var company=await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);var f=await rollbackImportacaoService.ExportarCsvAsync(id,company,cancellationToken);return File(f.Conteudo,f.ContentType,f.NomeArquivo);}

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

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }
}
