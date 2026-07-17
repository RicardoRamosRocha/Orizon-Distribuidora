using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Identity;

namespace Orizon.Distribuidora.Web.Services;

public interface ICurrentCompanyAccessor
{
    Task<Guid> GetCurrentCompanyIdAsync(ClaimsPrincipal user);
}

public sealed class CurrentCompanyAccessor : ICurrentCompanyAccessor
{
    private readonly ApplicationDbContext dbContext;
    private readonly IWebHostEnvironment environment;
    private readonly UserManager<ApplicationUser> userManager;

    public CurrentCompanyAccessor(
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        UserManager<ApplicationUser> userManager)
    {
        this.dbContext = dbContext;
        this.environment = environment;
        this.userManager = userManager;
    }

    public async Task<Guid> GetCurrentCompanyIdAsync(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated == true)
        {
            var applicationUser = await userManager.GetUserAsync(user);

            if (applicationUser?.CompanyId is Guid companyId &&
                companyId != Guid.Empty)
            {
                return companyId;
            }

            throw new UnauthorizedAccessException(
                "Usuário autenticado não possui empresa associada.");
        }

        if (!environment.IsDevelopment())
        {
            throw new UnauthorizedAccessException(
                "Não foi possível determinar a empresa atual.");
        }

        var fallbackCompanyId = await dbContext.Companies
            .OrderBy(company => company.CreatedAt)
            .Select(company => company.Id)
            .FirstOrDefaultAsync();

        if (fallbackCompanyId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Nenhuma empresa foi encontrada para o contexto atual.");
        }

        return fallbackCompanyId;
    }
}
