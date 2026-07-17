using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Models.Account;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Controllers;

[Route("[controller]")]
public sealed class AccountController : Controller
{
    private readonly ILogger<AccountController> logger;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.logger = logger;
    }

    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(
                AuthRedirectService.GetSafeLocalReturnUrl(Url, returnUrl));
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);

        if (user is null ||
            !user.IsActive ||
            user.CompanyId is null ||
            user.CompanyId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation(
                "Usuário {UserId} autenticado com sucesso.",
                user.Id);

            return LocalRedirect(
                AuthRedirectService.GetSafeLocalReturnUrl(Url, model.ReturnUrl));
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "A conta está temporariamente bloqueada. Tente novamente mais tarde.");

            return View(model);
        }

        ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
        return View(model);
    }

    [HttpPost("Logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }

    [HttpGet("AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
