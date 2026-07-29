using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Orizon.UI.Icons;
using Orizon.Distribuidora.Web.Controllers;
using Orizon.Distribuidora.Web.Models.Home;
using System.Text.RegularExpressions;

namespace Orizon.Distribuidora.Application.Tests.Dashboard;

public sealed class HomePrincipalPresentationTests
{
    [Fact]
    public void Root_home_is_explicitly_anonymous_and_has_safe_greeting_fallback()
    {
        var index = typeof(HomeController).GetMethod(nameof(HomeController.Index));

        Assert.NotNull(index);
        Assert.NotNull(index!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal("Bem-vindo à Orizon Distribuidora.", new HomeIndexViewModel().Greeting);
        Assert.Null(HomeIndexViewModel.GetSafeFirstName("usuario@orizon.local"));
        Assert.Equal("Maria", HomeIndexViewModel.GetSafeFirstName(" Maria Silva "));
    }

    [Fact]
    public void Main_home_uses_its_own_layout_and_real_authorized_routes()
    {
        var view = ReadRepositoryFile("src/Orizon.Distribuidora.Web/Views/Home/Index.cshtml");

        Assert.Contains("Layout = \"_HomeLayout\"", view);
        Assert.Contains("asp-controller=\"Dashboard\" asp-action=\"Index\"", view);
        Assert.Contains("asp-controller=\"Products\" asp-action=\"Index\"", view);
        Assert.Contains("asp-controller=\"Quotes\" asp-action=\"Index\"", view);
        Assert.Contains("asp-controller=\"Stock\" asp-action=\"Index\"", view);
        Assert.Contains("asp-controller=\"Importacao\" asp-action=\"Index\"", view);
        Assert.Contains("data-pdf-summer-open", view);
        Assert.Contains("@if (Model.CanAccessAdministration)", view);
        Assert.DoesNotContain("Orizon UI integrado", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"#\"", view);
    }

    [Fact]
    public void Home_layout_is_outside_admin_responsive_theme_aware_and_keeps_pdf_summer_functional()
    {
        var layout = ReadRepositoryFile("src/Orizon.Distribuidora.Web/Views/Shared/_HomeLayout.cshtml");
        var styles = ReadRepositoryFile("src/Orizon.Distribuidora.Web/wwwroot/css/home-premium.css");

        Assert.Contains("Orizon.UI/css/orizon.css", layout);
        Assert.Contains("data-theme-toggle", layout);
        Assert.Contains("name=\"_PdfSummer\"", layout);
        Assert.Contains("js/pdf-summer.js", layout);
        Assert.DoesNotContain("_Sidebar", layout);
        Assert.Contains("@media (max-width:", styles);
        Assert.Contains("var(--orizon-color-background)", styles);
        Assert.Contains("[data-theme=\"dark\"] .home-start", styles);
        Assert.Contains("data-color-mode=\"dark\"", styles);
    }

    [Fact]
    public void Pdf_calculator_has_the_new_visible_name_and_an_accessible_registered_launcher()
    {
        var home = ReadRepositoryFile("src/Orizon.Distribuidora.Web/Views/Home/Index.cshtml");
        var partial = ReadRepositoryFile("src/Orizon.Distribuidora.Web/Views/Shared/_PdfSummer.cshtml");
        var markup = home + partial;

        Assert.Contains("Calculadora de PDF", home);
        Assert.Contains("<h2 id=\"pdf-summer-title\">Calculadora de PDF</h2>", partial);
        Assert.Contains("aria-label=\"Abrir Calculadora de PDF\"", partial);
        Assert.Contains("title=\"Calculadora de PDF\"", partial);
        Assert.Contains("name=\"calculator\"", partial);
        Assert.Contains("aria-hidden=\"true\"", partial);
        Assert.DoesNotContain("Somador de PDF", markup, StringComparison.OrdinalIgnoreCase);

        Assert.True(OrizonIconRegistry.TryGet("calculator", out _),
            "O ícone 'calculator' do acionador não está registrado na Orizon.UI.");
    }

    [Fact]
    public void Pdf_calculator_launcher_is_fixed_draggable_bounded_and_persisted_safely()
    {
        var styles = ReadRepositoryFile("src/Orizon.Distribuidora.Web/wwwroot/css/pdf-summer.css");
        var script = ReadRepositoryFile("src/Orizon.Distribuidora.Web/wwwroot/js/pdf-summer.js");

        Assert.Matches(@"(?s)\.pdf-summer-launcher\s*\{[^}]*position:\s*fixed", styles);
        Assert.Contains("cursor: grab", styles);
        Assert.Contains("cursor: grabbing", styles);
        Assert.Contains("touch-action: none", styles);
        Assert.Contains("[data-theme=\"dark\"] .pdf-summer-launcher", styles);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", styles);
        Assert.Contains("env(safe-area-inset-", styles);

        Assert.Contains("\"orizon.pdf-calculator.floating-position.v1\"", script);
        Assert.Contains("launcherDragThreshold = 5", script);
        Assert.Contains("Math.hypot(deltaX, deltaY)", script);
        Assert.Contains("setPointerCapture", script);
        Assert.Contains("releasePointerCapture", script);
        Assert.Contains("\"pointercancel\"", script);
        Assert.Contains("constrainLauncherPosition", script);
        Assert.Contains("window.innerWidth", script);
        Assert.Contains("window.innerHeight", script);
        Assert.Contains("\"orientationchange\"", script);
        Assert.Contains("Number.isFinite(value.x)", script);
        Assert.Contains("value.x < 0 || value.x > 1", script);
        Assert.Contains("localStorage.setItem(launcherPositionStorageKey", script);
        Assert.DoesNotContain("localStorage.clear()", script);
        Assert.Contains("if (suppressLauncherClick)", script);
        Assert.Contains("launcher.addEventListener(\"click\"", script);
    }

    [Fact]
    public void Home_icons_are_registered_and_commercial_and_administrative_icons_do_not_regress()
    {
        var view = ReadRepositoryFile("src/Orizon.Distribuidora.Web/Views/Home/Index.cshtml");
        var layout = ReadRepositoryFile("src/Orizon.Distribuidora.Web/Views/Shared/_HomeLayout.cshtml");
        var iconNames = ExtractLiteralIconNames(view).Concat(ExtractLiteralIconNames(layout)).ToArray();

        Assert.NotEmpty(iconNames);
        Assert.All(iconNames, name =>
            Assert.True(OrizonIconRegistry.TryGet(name, out _), $"O ícone '{name}' não está registrado na Orizon.UI."));
        Assert.Contains("name=\"clipboard\"", view);
        Assert.Contains("name=\"dashboard\"", view);
        Assert.DoesNotContain("name=\"file-text\"", view);
        Assert.DoesNotContain("name=\"compass\"", view);
        Assert.DoesNotContain("name=\"\"", view);
        Assert.DoesNotContain("<orizon-icon></orizon-icon>", view);
        Assert.DoesNotContain("<orizon-icon></orizon-icon>", layout);
    }

    [Fact]
    public void Administrative_dashboard_remains_the_existing_executive_dashboard()
    {
        var view = ReadRepositoryFile(
            "src/Orizon.Distribuidora.Web/Areas/Admin/Views/Dashboard/Index.cshtml");

        Assert.Contains("data-dashboard", view);
        Assert.Contains("Visão geral", view);
        Assert.Contains("DemoPeriods", view);
        Assert.DoesNotContain("home-premium", view);
        Assert.DoesNotContain("Acessar área administrativa", view);
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Orizon.Distribuidora.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }

    private static IEnumerable<string> ExtractLiteralIconNames(string markup) =>
        Regex.Matches(markup, "<orizon-icon\\b[^>]*\\bname=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value)
            .Where(name => !name.StartsWith('@'));
}
