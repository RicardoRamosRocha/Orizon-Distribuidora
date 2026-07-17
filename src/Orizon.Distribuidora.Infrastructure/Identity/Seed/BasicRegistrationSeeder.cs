using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Identity.Seed;

public static class BasicRegistrationSeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var company = await dbContext.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(company => company.Slug == "orizon-distribuidora");

        if (company is null)
        {
            company = new Company(
                "Orizon Distribuidora Ltda",
                "Orizon Distribuidora",
                "00000000000191",
                "orizon-distribuidora");

            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync();
        }

        var adminEmail = configuration["Seed:Administrator:Email"];

        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var administrator = await userManager.FindByEmailAsync(adminEmail);

            if (administrator is not null &&
                administrator.CompanyId is null)
            {
                administrator.CompanyId = company.Id;
                await userManager.UpdateAsync(administrator);
            }
        }

        await SeedUnitsOfMeasureAsync(dbContext, company.Id);
        await SeedRegistrationStatusesAsync(dbContext, company.Id);
    }

    private static async Task SeedUnitsOfMeasureAsync(
        ApplicationDbContext dbContext,
        Guid companyId)
    {
        var units = new[]
        {
            new { Name = "Unidade", Abbreviation = "UN", DecimalPlaces = 0, AllowsFraction = false },
            new { Name = "Caixa", Abbreviation = "CX", DecimalPlaces = 0, AllowsFraction = false },
            new { Name = "Quilograma", Abbreviation = "KG", DecimalPlaces = 3, AllowsFraction = true },
            new { Name = "Grama", Abbreviation = "G", DecimalPlaces = 3, AllowsFraction = true },
            new { Name = "Metro", Abbreviation = "M", DecimalPlaces = 3, AllowsFraction = true },
            new { Name = "Metro quadrado", Abbreviation = "M²", DecimalPlaces = 3, AllowsFraction = true },
            new { Name = "Metro cúbico", Abbreviation = "M³", DecimalPlaces = 3, AllowsFraction = true },
            new { Name = "Litro", Abbreviation = "L", DecimalPlaces = 3, AllowsFraction = true }
        };

        foreach (var unit in units)
        {
            var exists = await dbContext.UnitsOfMeasure
                .AnyAsync(entity =>
                    entity.CompanyId == companyId &&
                    entity.Abbreviation == unit.Abbreviation);

            if (exists)
            {
                continue;
            }

            dbContext.UnitsOfMeasure.Add(
                new UnitOfMeasure(
                    companyId,
                    unit.Name,
                    unit.Abbreviation,
                    null,
                    unit.DecimalPlaces,
                    unit.AllowsFraction));
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRegistrationStatusesAsync(
        ApplicationDbContext dbContext,
        Guid companyId)
    {
        var statuses = new[]
        {
            new { Name = "Ativo", Code = "ATIVO", Color = "#16A34A", SortOrder = 1 },
            new { Name = "Em análise", Code = "EM_ANALISE", Color = "#F59E0B", SortOrder = 2 },
            new { Name = "Bloqueado", Code = "BLOQUEADO", Color = "#DC2626", SortOrder = 3 },
            new { Name = "Descontinuado", Code = "DESCONTINUADO", Color = "#64748B", SortOrder = 4 }
        };

        foreach (var status in statuses)
        {
            var exists = await dbContext.RegistrationStatuses
                .AnyAsync(entity =>
                    entity.CompanyId == companyId &&
                    entity.Code == status.Code);

            if (exists)
            {
                continue;
            }

            dbContext.RegistrationStatuses.Add(
                new RegistrationStatus(
                    companyId,
                    status.Name,
                    status.Code,
                    null,
                    status.Color,
                    status.SortOrder,
                    isSystem: true));
        }

        await dbContext.SaveChangesAsync();
    }
}
