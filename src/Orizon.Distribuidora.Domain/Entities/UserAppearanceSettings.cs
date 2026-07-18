using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class UserAppearanceSettings : CompanyOwnedAuditableEntity
{
    private static readonly HashSet<string> Modes = ["light", "dark", "auto"];
    private static readonly HashSet<string> Densities = ["compact", "normal", "comfortable"];
    private static readonly HashSet<string> Sidebars = ["expanded", "collapsed", "icons"];
    private static readonly HashSet<string> FontSizes = ["small", "normal", "large"];
    private static readonly HashSet<string> Radii = ["square", "soft", "rounded"];
    private static readonly HashSet<string> Shadows = ["minimal", "standard", "elevated"];
    private static readonly HashSet<string> Motions = ["normal", "reduced", "none"];

    private UserAppearanceSettings() { }
    public UserAppearanceSettings(Guid companyId, Guid userId) : base(companyId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Usuario obrigatorio.", nameof(userId));
        UserId = userId;
    }

    public Guid UserId { get; private set; }
    public string? Theme { get; private set; }
    public string? PrimaryColor { get; private set; }
    public string Mode { get; private set; } = "auto";
    public string Density { get; private set; } = "normal";
    public string Sidebar { get; private set; } = "expanded";
    public string FontSize { get; private set; } = "normal";
    public string Radius { get; private set; } = "rounded";
    public string Shadow { get; private set; } = "standard";
    public string Motion { get; private set; } = "normal";
    public string Language { get; private set; } = "pt-BR";

    public void Update(string? theme, string? primaryColor, string mode, string density, string sidebar, string fontSize, string radius, string shadow, string motion, string? language)
    {
        Theme = string.IsNullOrWhiteSpace(theme) ? null : AppearanceOptions.Theme(theme);
        PrimaryColor = string.IsNullOrWhiteSpace(primaryColor) ? null : AppearanceOptions.PrimaryColor(primaryColor);
        Mode = AppearanceOptions.Option(mode, Modes, "Modo");
        Density = AppearanceOptions.Option(density, Densities, "Densidade");
        Sidebar = AppearanceOptions.Option(sidebar, Sidebars, "Sidebar");
        FontSize = AppearanceOptions.Option(fontSize, FontSizes, "Tipografia");
        Radius = AppearanceOptions.Option(radius, Radii, "Borda");
        Shadow = AppearanceOptions.Option(shadow, Shadows, "Sombra");
        Motion = AppearanceOptions.Option(motion, Motions, "Animacao");
        Language = AppearanceOptions.OptionalText(language, 10) ?? "pt-BR";
    }
}
