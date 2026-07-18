using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class AppearanceSettingsTests
{
    [Fact]
    public void CompanyAppearance_ShouldPersistBrandIdentity()
    {
        var settings = new CompanyAppearanceSettings(Guid.NewGuid());
        settings.Update("corporate", "purple", "/logo.svg", "/favicon.ico", "/login.webp", "Portal Orizon");
        Assert.Equal("corporate", settings.Theme);
        Assert.Equal("purple", settings.PrimaryColor);
        Assert.Equal("/logo.svg", settings.LogoPath);
        Assert.Equal("Portal Orizon", settings.LoginTitle);
    }

    [Theory]
    [InlineData("unknown", "blue")]
    [InlineData("light", "pink")]
    public void CompanyAppearance_ShouldRejectUnsupportedTokens(string theme, string color)
    {
        var settings = new CompanyAppearanceSettings(Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => settings.Update(theme, color, null, null, null, null));
    }

    [Fact]
    public void UserAppearance_ShouldPersistEveryOverride()
    {
        var settings = new UserAppearanceSettings(Guid.NewGuid(), Guid.NewGuid());
        settings.Update("hair", "red", "dark", "compact", "icons", "large", "soft", "elevated", "reduced", "pt-BR");
        Assert.Equal("hair", settings.Theme);
        Assert.Equal("dark", settings.Mode);
        Assert.Equal("compact", settings.Density);
        Assert.Equal("icons", settings.Sidebar);
        Assert.Equal("large", settings.FontSize);
        Assert.Equal("soft", settings.Radius);
        Assert.Equal("elevated", settings.Shadow);
        Assert.Equal("reduced", settings.Motion);
    }

    [Fact]
    public void UserAppearance_ShouldAllowCompanyThemeInheritance()
    {
        var settings = new UserAppearanceSettings(Guid.NewGuid(), Guid.NewGuid());
        settings.Update(null, null, "auto", "normal", "expanded", "normal", "rounded", "standard", "normal", null);
        Assert.Null(settings.Theme);
        Assert.Null(settings.PrimaryColor);
        Assert.Equal("pt-BR", settings.Language);
    }
}
