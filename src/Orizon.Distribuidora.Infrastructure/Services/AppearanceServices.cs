using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Appearance;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class AppearanceService(ApplicationDbContext dbContext) : IAppearanceService
{
    public async Task<AppearancePreferences> GetAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var company = await dbContext.CompanyAppearanceSettings.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        var user = await dbContext.UserAppearanceSettings.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId, cancellationToken);
        return new(
            user?.Theme ?? company?.Theme ?? "light", user?.PrimaryColor ?? company?.PrimaryColor ?? "blue",
            user?.Mode ?? "auto", user?.Density ?? "normal", user?.Sidebar ?? "expanded", user?.FontSize ?? "normal",
            user?.Radius ?? "rounded", user?.Shadow ?? "standard", user?.Motion ?? "normal", user?.Language ?? "pt-BR",
            company?.LogoPath, company?.FaviconPath, company?.LoginBackgroundPath, company?.LoginTitle);
    }
}

public sealed class ThemeService(ApplicationDbContext dbContext, IAppearanceService appearanceService) : IThemeService
{
    private static readonly ThemeDefinition[] Themes =
    [
        new("light", "Light", "Claro e minimalista", false), new("dark", "Dark", "Escuro com contraste premium", true),
        new("corporate", "Corporate", "Institucional e sofisticado", false), new("construction", "Construction", "Energia para operacoes", false),
        new("real-estate", "Real Estate", "Elegante para negocios imobiliarios", false), new("hair", "Hair", "Expressivo para beleza", false),
        new("agents", "Agents", "Tecnologico e orientado a IA", true), new("renova", "Renova", "Sustentavel e contemporaneo", false)
    ];
    public IReadOnlyList<ThemeDefinition> GetThemes() => Themes;

    public async Task<AppearancePreferences> SaveCompanyAsync(Guid companyId, AppearancePreferences value, CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.CompanyAppearanceSettings.FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        if (settings is null) { settings = new CompanyAppearanceSettings(companyId); dbContext.CompanyAppearanceSettings.Add(settings); }
        settings.Update(value.Theme, value.PrimaryColor, value.LogoPath, value.FaviconPath, value.LoginBackgroundPath, value.LoginTitle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await appearanceService.GetAsync(companyId, Guid.Empty, cancellationToken);
    }
}

public sealed class PreferenceService(ApplicationDbContext dbContext, IAppearanceService appearanceService) : IPreferenceService
{
    public async Task<AppearancePreferences> SaveUserAsync(Guid companyId, Guid userId, AppearancePreferences value, CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.UserAppearanceSettings.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId, cancellationToken);
        if (settings is null) { settings = new UserAppearanceSettings(companyId, userId); dbContext.UserAppearanceSettings.Add(settings); }
        settings.Update(value.Theme, value.PrimaryColor, value.Mode, value.Density, value.Sidebar, value.FontSize, value.Radius, value.Shadow, value.Motion, value.Language);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await appearanceService.GetAsync(companyId, userId, cancellationToken);
    }
}

public sealed class ThemeStorageService(IWebHostEnvironment environment) : IThemeStorageService
{
    private static readonly HashSet<string> Extensions = [".png", ".jpg", ".jpeg", ".webp", ".svg", ".ico"];
    public async Task<string> SaveAsync(Guid companyId, Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!Extensions.Contains(extension) || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Arquivo de imagem invalido.");
        var directory = Path.Combine(environment.WebRootPath, "uploads", "appearance", companyId.ToString("N"));
        Directory.CreateDirectory(directory);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        await using var target = File.Create(Path.Combine(directory, storedName));
        await content.CopyToAsync(target, cancellationToken);
        return $"/uploads/appearance/{companyId:N}/{storedName}";
    }
}
