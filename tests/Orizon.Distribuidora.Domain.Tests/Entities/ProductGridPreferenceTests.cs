using Orizon.Distribuidora.Domain.Entities;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class ProductGridPreferenceTests
{
    [Fact]
    public void Preference_ShouldBelongToCompanyAndUser()
    {
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var preference = new ProductGridPreference(companyId, userId, "{\"columns\":[]}");

        Assert.Equal(companyId, preference.CompanyId);
        Assert.Equal(userId, preference.UserId);
        Assert.Contains("columns", preference.StateJson);
    }

    [Fact]
    public void SavedFilter_ShouldValidateAndNormalizeName()
    {
        var filter = new ProductSavedFilter(Guid.NewGuid(), Guid.NewGuid(), "  Ativos  ", "{}");

        Assert.Equal("Ativos", filter.Name);
        Assert.Throws<ArgumentException>(() => filter.Rename(string.Empty));
    }
}
