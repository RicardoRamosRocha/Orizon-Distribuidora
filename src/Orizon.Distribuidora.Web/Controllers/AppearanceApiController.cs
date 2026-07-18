using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Application.Appearance;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Appearance;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class AppearanceApiController(ICurrentCompanyAccessor companyAccessor, IAppearanceService appearanceService, IThemeService themeService, IPreferenceService preferenceService, IThemeStorageService storageService) : ControllerBase
{
    [HttpGet("appearance")]
    public async Task<ActionResult<AppearancePreferences>> GetAppearance(CancellationToken cancellationToken) =>
        Ok(await appearanceService.GetAsync(await CompanyId(), UserId(), cancellationToken));

    [HttpGet("themes")]
    public ActionResult<IReadOnlyList<ThemeDefinition>> GetThemes() => Ok(themeService.GetThemes());

    [HttpPost("theme")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AppearancePreferences>> SaveTheme([FromBody] AppearancePreferences request, CancellationToken cancellationToken)
    {
        var companyId = await CompanyId();
        var current = await appearanceService.GetAsync(companyId, UserId(), cancellationToken);
        return Ok(await preferenceService.SaveUserAsync(companyId, UserId(), current with { Theme = request.Theme, PrimaryColor = request.PrimaryColor, Mode = request.Mode }, cancellationToken));
    }

    [HttpPost("preferences")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AppearancePreferences>> SavePreferences([FromBody] AppearancePreferences request, CancellationToken cancellationToken) =>
        Ok(await preferenceService.SaveUserAsync(await CompanyId(), UserId(), request, cancellationToken));

    [HttpPost("company/theme")]
    [Authorize(Roles = Roles.Administrator)]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AppearancePreferences>> SaveCompany([FromBody] AppearancePreferences request, CancellationToken cancellationToken) =>
        Ok(await themeService.SaveCompanyAsync(await CompanyId(), request, cancellationToken));

    [HttpPost("company/assets")]
    [Authorize(Roles = Roles.Administrator)]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> UploadAsset(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > 8 * 1024 * 1024) return BadRequest(new { message = "Imagem invalida ou maior que 8 MB." });
        await using var stream = file.OpenReadStream();
        var path = await storageService.SaveAsync(await CompanyId(), stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(new { path });
    }

    private Task<Guid> CompanyId() => companyAccessor.GetCurrentCompanyIdAsync(User);
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
