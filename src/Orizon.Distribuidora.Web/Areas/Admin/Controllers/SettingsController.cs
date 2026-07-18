using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orizon.Distribuidora.Infrastructure.Identity;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
public sealed class SettingsController : Controller
{
    [HttpGet("Admin/Settings")]
    public IActionResult Index() => View();

    [HttpGet("Admin/{section:regex(Company|Users|Roles|Audit|Preferences|Help)}")]
    public IActionResult Section(string section)
    {
        var content = section switch
        {
            "Company" => ("Empresa", "Dados e identidade da empresa"),
            "Users" => ("Usuários", "Gestão de acessos da equipe"),
            "Roles" => ("Perfis", "Perfis e permissões de acesso"),
            "Audit" => ("Auditoria", "Histórico de atividades administrativas"),
            "Preferences" => ("Preferências", "Preferências gerais da plataforma"),
            _ => ("Ajuda", "Central de ajuda da plataforma")
        };
        ViewData["Title"] = content.Item1;
        ViewData["Description"] = content.Item2;
        ViewData["BreadcrumbSection"] = section == "Help" ? null : "Configurações";
        return View("Section");
    }
}
