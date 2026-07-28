using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Dashboard;
using Orizon.Distribuidora.Web.Services;

namespace Orizon.Distribuidora.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Administrator)]
public sealed class DashboardController : Controller
{
    private readonly ApplicationDbContext dbContext;
    private readonly ICurrentCompanyAccessor currentCompanyAccessor;

    public DashboardController(
        ApplicationDbContext dbContext,
        ICurrentCompanyAccessor currentCompanyAccessor)
    {
        this.dbContext = dbContext;
        this.currentCompanyAccessor = currentCompanyAccessor;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var companyId = await currentCompanyAccessor.GetCurrentCompanyIdAsync(User);
        var products = dbContext.Products.AsNoTracking()
            .Where(product => product.CompanyId == companyId);

        var summary = await products.GroupBy(_ => 1)
            .Select(group => new
            {
                Active = group.Count(product => product.IsActive),
                Controlled = group.Count(product => product.IsActive && product.ControlsStock),
                CatalogValue = group.Where(product => product.IsActive).Sum(product => product.SalePrice),
                AverageMargin = group.Where(product => product.IsActive && product.SalePrice > 0)
                    .Average(product => (decimal?)((product.SalePrice - product.CostPrice) / product.SalePrice * 100))
            })
            .SingleOrDefaultAsync(cancellationToken);

        var categoryRows = await products.Where(product => product.IsActive)
            .GroupBy(product => product.Category != null ? product.Category.Name : "Sem categoria")
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .Take(6)
            .ToListAsync(cancellationToken);
        var categories = categoryRows
            .Select(item => new DashboardCategoryItem(item.Name, item.Count))
            .ToList();

        var featuredRows = await products.Where(product => product.IsActive)
            .OrderByDescending(product => product.SalePrice > 0
                ? (product.SalePrice - product.CostPrice) / product.SalePrice
                : 0)
            .ThenBy(product => product.Name)
            .Take(5)
            .Select(product => new
            {
                product.Id,
                Code = product.InternalCode,
                product.Name,
                Category = product.Category != null ? product.Category.Name : "Sem categoria",
                Price = product.SalePrice,
                Margin = product.SalePrice > 0
                    ? (product.SalePrice - product.CostPrice) / product.SalePrice * 100
                    : 0
            })
            .ToListAsync(cancellationToken);
        var featured = featuredRows.Select(product => new DashboardProductItem(
            product.Id, product.Code, product.Name, product.Category, product.Price,
            Math.Round(product.Margin, 2))).ToList();

        var activityRows = await dbContext.ProductChangeHistories.AsNoTracking()
            .Where(item => item.CompanyId == companyId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(6)
            .Select(item => new
            {
                Title = item.FieldName == "Produto" ? "Produto cadastrado" : "Produto atualizado",
                Description = item.Product != null
                    ? item.Product.Name + " · " + item.FieldName
                    : item.FieldName,
                item.CreatedAt,
                item.Origin
            })
            .ToListAsync(cancellationToken);
        var activities = activityRows.Select(item => new DashboardActivityItem(
            item.Title, item.Description, item.CreatedAt, item.Origin)).ToList();

        var displayName = User.Identity?.Name?.Split('@')[0]?.Split(' ')[0];
        var model = new DashboardViewModel
        {
            UpdatedAt = DateTimeOffset.UtcNow,
            UserFirstName = string.IsNullOrWhiteSpace(displayName) ? "Administrador" : displayName,
            ActiveProducts = summary?.Active ?? 0,
            StockControlledProducts = summary?.Controlled ?? 0,
            CatalogValue = summary?.CatalogValue ?? 0,
            AverageMargin = Math.Round(summary?.AverageMargin ?? 0, 1),
            Categories = categories,
            FeaturedProducts = featured,
            RecentActivities = activities,
            DemoPeriods = BuildDemoPeriods()
        };

        return View(model);
    }

    private static IReadOnlyDictionary<string, DashboardPeriodData> BuildDemoPeriods()
    {
        static DashboardPeriodData Period(
            string label, decimal revenue, int sales, decimal revenueChange, decimal salesChange,
            string[] labels, decimal[] revenueSeries, decimal[] previousRevenueSeries, int[] salesSeries,
            decimal factor = 1)
        {
            var ticket = sales == 0 ? 0 : Math.Round(revenue / sales, 2);
            return new(
                label, revenue, sales, ticket, revenueChange, salesChange,
                labels, revenueSeries, previousRevenueSeries, salesSeries,
                [
                    new("Cimentos e argamassas", Math.Round(revenue * .34m)),
                    new("Hidráulica", Math.Round(revenue * .24m)),
                    new("Elétrica", Math.Round(revenue * .19m)),
                    new("Ferramentas", Math.Round(revenue * .14m)),
                    new("Outros", Math.Round(revenue * .09m))
                ],
                [
                    new("Cimento CP II 50 kg", Math.Round(18450 * factor)),
                    new("Argamassa AC-II 20 kg", Math.Round(14280 * factor)),
                    new("Tubo PVC soldável 25 mm", Math.Round(11940 * factor)),
                    new("Cabo flexível 2,5 mm", Math.Round(9860 * factor)),
                    new("Tinta acrílica premium 18 L", Math.Round(8350 * factor))
                ],
                Math.Round(revenue * .58m),
                Math.Round(revenue * .31m),
                Math.Round(revenue * .07m));
        }

        return new Dictionary<string, DashboardPeriodData>
        {
            ["today"] = Period("Hoje", 18640, 42, 8.4m, 5.0m,
                ["08h", "10h", "12h", "14h", "16h", "18h"],
                [1540, 3280, 2710, 4160, 3890, 3060], [1320, 2860, 2430, 3550, 3410, 3230], [4, 8, 6, 10, 8, 6], .22m),
            ["7days"] = Period("Últimos 7 dias", 128760, 286, 12.8m, 9.2m,
                ["Seg", "Ter", "Qua", "Qui", "Sex", "Sáb", "Dom"],
                [14600, 17800, 16400, 22100, 24600, 19800, 13460],
                [13200, 15100, 15800, 19200, 21600, 17500, 11740], [34, 39, 37, 48, 54, 43, 31], 1.45m),
            ["30days"] = Period("Últimos 30 dias", 548920, 1218, 14.6m, 11.3m,
                ["01–05", "06–10", "11–15", "16–20", "21–25", "26–30"],
                [82400, 91600, 87500, 101300, 97820, 88300],
                [75800, 81100, 80200, 87900, 90100, 63890], [183, 204, 193, 224, 218, 196], 6.1m),
            ["month"] = Period("Este mês", 426380, 947, 10.9m, 8.1m,
                ["Sem 1", "Sem 2", "Sem 3", "Sem 4"],
                [98500, 104800, 116400, 106680], [91200, 97800, 101900, 93570], [219, 232, 258, 238], 4.8m)
        };
    }
}
