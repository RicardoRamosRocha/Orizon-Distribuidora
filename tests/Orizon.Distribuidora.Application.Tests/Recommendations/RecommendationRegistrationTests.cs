using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Infrastructure.DependencyInjection;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Recommendations;

public sealed class RecommendationRegistrationTests
{
    [Fact]
    public void Infrastructure_registers_recommendation_service_as_scoped()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=orizon_distribuidora;Username=postgres;Password=postgres"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);

        var registration = Assert.Single(services.Where(item => item.ServiceType == typeof(IRecommendationService)));
        Assert.Equal(typeof(RecommendationService), registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }
}
