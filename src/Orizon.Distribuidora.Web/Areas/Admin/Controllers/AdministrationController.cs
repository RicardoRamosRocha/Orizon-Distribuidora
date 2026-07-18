using Microsoft.AspNetCore.Mvc;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/Administration")]
public class AdministrationController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("Company")]
    public IActionResult Company() => View();

    [HttpGet("Branches")]
    public IActionResult Branches() => View();

    [HttpGet("Users")]
    public IActionResult Users() => View();

    [HttpGet("Roles")]
    public IActionResult Roles() => View();

    [HttpGet("Permissions")]
    public IActionResult Permissions() => View();

    [HttpGet("ProductSettings")]
    public IActionResult ProductSettings() => View();

    [HttpGet("InventorySettings")]
    public IActionResult InventorySettings() => View();

    [HttpGet("PriceSettings")]
    public IActionResult PriceSettings() => View();

    [HttpGet("SalesSettings")]
    public IActionResult SalesSettings() => View();

    [HttpGet("FinanceSettings")]
    public IActionResult FinanceSettings() => View();

    [HttpGet("Integrations")]
    public IActionResult Integrations() => View();

    [HttpGet("Database")]
    public IActionResult Database() => View();

    [HttpGet("Backup")]
    public IActionResult Backup() => View();

    [HttpGet("Logs")]
    public IActionResult Logs() => View();

    [HttpGet("Audit")]
    public IActionResult Audit() => View();

    [HttpGet("Sessions")]
    public IActionResult Sessions() => View();

    [HttpGet("Tokens")]
    public IActionResult Tokens() => View();

    [HttpGet("General")]
    public IActionResult General() => View();
}