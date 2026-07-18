using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Application.Appearance;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Appearance;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
[Route("Admin/Appearance")]
public sealed class AppearanceController(ICurrentCompanyAccessor companyAccessor, IAppearanceService appearanceService, IThemeService themeService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var companyId = await companyAccessor.GetCurrentCompanyIdAsync(User);
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(new AppearancePageViewModel { Preferences = await appearanceService.GetAsync(companyId, userId, cancellationToken), Themes = themeService.GetThemes() });
    }
}
