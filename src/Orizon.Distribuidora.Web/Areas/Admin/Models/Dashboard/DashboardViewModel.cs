namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Dashboard;

public sealed class DashboardViewModel
{
    public DateTimeOffset UpdatedAt { get; init; }
    public string UserFirstName { get; init; } = "Administrador";
    public int ActiveProducts { get; init; }
    public int StockControlledProducts { get; init; }
    public decimal CatalogValue { get; init; }
    public decimal AverageMargin { get; init; }
    public IReadOnlyList<DashboardCategoryItem> Categories { get; init; } = [];
    public IReadOnlyList<DashboardProductItem> FeaturedProducts { get; init; } = [];
    public IReadOnlyList<DashboardActivityItem> RecentActivities { get; init; } = [];
    public IReadOnlyDictionary<string, DashboardPeriodData> DemoPeriods { get; init; } =
        new Dictionary<string, DashboardPeriodData>();
}

public sealed record DashboardCategoryItem(string Name, int Count);

public sealed record DashboardProductItem(
    Guid Id,
    string Code,
    string Name,
    string Category,
    decimal Price,
    decimal Margin);

public sealed record DashboardActivityItem(
    string Title,
    string Description,
    DateTimeOffset OccurredAt,
    string Kind);

public sealed record DashboardPeriodData(
    string Label,
    decimal Revenue,
    int Sales,
    decimal AverageTicket,
    decimal RevenueChange,
    decimal SalesChange,
    IReadOnlyList<string> Labels,
    IReadOnlyList<decimal> RevenueSeries,
    IReadOnlyList<decimal> PreviousRevenueSeries,
    IReadOnlyList<int> SalesSeries,
    IReadOnlyList<DashboardDemoSlice> SalesComposition,
    IReadOnlyList<DashboardDemoRanking> TopProducts,
    decimal Received,
    decimal Receivable,
    decimal Overdue);

public sealed record DashboardDemoSlice(string Label, decimal Value);

public sealed record DashboardDemoRanking(string Label, decimal Value);
