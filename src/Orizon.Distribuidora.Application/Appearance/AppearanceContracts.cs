namespace Orizon.Distribuidora.Application.Appearance;

public sealed record AppearancePreferences(
    string Theme = "light", string PrimaryColor = "blue", string Mode = "auto",
    string Density = "normal", string Sidebar = "expanded", string FontSize = "normal",
    string Radius = "rounded", string Shadow = "standard", string Motion = "normal",
    string Language = "pt-BR", string? LogoPath = null, string? FaviconPath = null,
    string? LoginBackgroundPath = null, string? LoginTitle = null);

public sealed record ThemeDefinition(string Id, string Name, string Description, bool Dark);

public interface IAppearanceService
{
    Task<AppearancePreferences> GetAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default);
}

public interface IThemeService
{
    IReadOnlyList<ThemeDefinition> GetThemes();
    Task<AppearancePreferences> SaveCompanyAsync(Guid companyId, AppearancePreferences preferences, CancellationToken cancellationToken = default);
}

public interface IPreferenceService
{
    Task<AppearancePreferences> SaveUserAsync(Guid companyId, Guid userId, AppearancePreferences preferences, CancellationToken cancellationToken = default);
}

public interface IThemeStorageService
{
    Task<string> SaveAsync(Guid companyId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}
