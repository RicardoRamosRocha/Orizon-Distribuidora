using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class CompanyAppearanceSettings : CompanyOwnedAuditableEntity
{
    private CompanyAppearanceSettings() { }
    public CompanyAppearanceSettings(Guid companyId) : base(companyId) { }

    public string Theme { get; private set; } = "light";
    public string PrimaryColor { get; private set; } = "blue";
    public string? LogoPath { get; private set; }
    public string? FaviconPath { get; private set; }
    public string? LoginBackgroundPath { get; private set; }
    public string? LoginTitle { get; private set; }

    public void Update(string theme, string primaryColor, string? logoPath, string? faviconPath, string? loginBackgroundPath, string? loginTitle)
    {
        Theme = AppearanceOptions.Theme(theme);
        PrimaryColor = AppearanceOptions.PrimaryColor(primaryColor);
        LogoPath = AppearanceOptions.OptionalPath(logoPath);
        FaviconPath = AppearanceOptions.OptionalPath(faviconPath);
        LoginBackgroundPath = AppearanceOptions.OptionalPath(loginBackgroundPath);
        LoginTitle = AppearanceOptions.OptionalText(loginTitle, 120);
    }
}

internal static class AppearanceOptions
{
    private static readonly HashSet<string> Themes = ["light", "dark", "corporate", "construction", "real-estate", "hair", "agents", "renova"];
    private static readonly HashSet<string> Colors = ["blue", "green", "purple", "red", "orange", "cyan"];
    public static string Theme(string value) => Themes.Contains(value) ? value : throw new ArgumentException("Tema invalido.");
    public static string PrimaryColor(string value) => Colors.Contains(value) ? value : throw new ArgumentException("Cor primaria invalida.");
    public static string Option(string value, IReadOnlySet<string> allowed, string name) => allowed.Contains(value) ? value : throw new ArgumentException($"{name} invalido.");
    public static string? OptionalPath(string? value) => string.IsNullOrWhiteSpace(value) ? null : OptionalText(value, 400);
    public static string? OptionalText(string? value, int length) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= length ? value.Trim() : throw new ArgumentException("Texto excede o limite permitido.");
}
