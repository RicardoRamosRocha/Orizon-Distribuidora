using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Stock;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Stock;

public sealed class StockServicePostgreSqlTests
{
    [Fact]
    public async Task ListStockBalancesAsync_translates_count_and_materialization_on_postgresql()
    {
        var connectionString = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var db = new ApplicationDbContext(options);
        var companyId = await db.Companies.AsNoTracking()
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await new StockService(db).ListStockBalancesAsync(
            companyId,
            new StockBalanceFilter(
                OnlyActive: true,
                Page: 1,
                PageSize: 10,
                Search: "a",
                Status: StockLevelStatus.Normal,
                SortBy: "quantity",
                SortDirection: "desc"),
            cancellation.Token);

        Assert.InRange(result.Items.Count, 0, 10);
        Assert.True(result.TotalCount >= result.Items.Count);
    }
}
