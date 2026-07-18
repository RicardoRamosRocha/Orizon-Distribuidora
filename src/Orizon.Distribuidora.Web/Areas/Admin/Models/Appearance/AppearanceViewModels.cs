using Orizon.Distribuidora.Application.Appearance;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Appearance;

public sealed class AppearancePageViewModel
{
    public AppearancePreferences Preferences { get; init; } = new();
    public IReadOnlyList<ThemeDefinition> Themes { get; init; } = [];
}

public sealed class CompanyAppearanceForm
{
    public string Theme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "blue";
    public string? LogoPath { get; set; }
    public string? FaviconPath { get; set; }
    public string? LoginBackgroundPath { get; set; }
    public string? LoginTitle { get; set; }
}
