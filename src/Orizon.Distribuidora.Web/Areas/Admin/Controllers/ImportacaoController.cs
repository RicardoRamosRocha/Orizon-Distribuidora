using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Importacoes;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
[Route("Admin/Importacao")]
public sealed class ImportacaoController : Controller
{
    private readonly IHistoricoImportacaoService historicoImportacaoService;
    private readonly ICurrentCompanyAccessor currentCompanyAccessor;

    public ImportacaoController(
        IHistoricoImportacaoService historicoImportacaoService,
        ICurrentCompanyAccessor currentCompanyAccessor)
    {
        this.historicoImportacaoService = historicoImportacaoService;
        this.currentCompanyAccessor = currentCompanyAccessor;
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
        return View();
    }

    [HttpGet("Preview")]
    public IActionResult Preview()
    {
        return View();
    }

    [HttpGet("Resultado")]
    public IActionResult Resultado()
    {
        return View();
    }

    [HttpGet("Historico")]
    public async Task<IActionResult> Historico()
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var historico = await historicoImportacaoService.ListarAsync(companyId);
        var model = historico
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
            .ToList();

        return View(model);
    }
}
