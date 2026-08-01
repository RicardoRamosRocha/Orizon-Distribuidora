using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class RecommendationTests
{
    [Fact]
    public void Constructor_normalizes_data_and_preserves_module_context()
    {
        var companyId = Guid.NewGuid();
        var recommendation = new Recommendation(
            companyId,
            " Estoque ",
            RecommendationType.Opportunity,
            RecommendationSeverity.Medium,
            " Repor produto ",
            " Saldo abaixo do ideal. ",
            "product:42",
            "/products/42",
            "{\"quantity\":10}");

        Assert.Equal(companyId, recommendation.CompanyId);
        Assert.Equal("Estoque", recommendation.Module);
        Assert.Equal("Repor produto", recommendation.Title);
        Assert.Equal("Saldo abaixo do ideal.", recommendation.Description);
        Assert.Equal("product:42", recommendation.ReferenceId);
        Assert.Null(recommendation.ReadAt);
        Assert.Null(recommendation.DismissedAt);
    }

    [Fact]
    public void Read_and_dismiss_actions_are_idempotent_and_audited()
    {
        var recommendation = CreateRecommendation();
        var readerId = Guid.NewGuid();
        var firstRead = DateTimeOffset.UtcNow.AddMinutes(-2);

        recommendation.MarkAsRead(readerId, firstRead);
        recommendation.MarkAsRead(Guid.NewGuid(), firstRead.AddMinutes(1));
        recommendation.Dismiss(readerId, firstRead.AddMinutes(2));

        Assert.Equal(firstRead, recommendation.ReadAt);
        Assert.Equal(firstRead.AddMinutes(2), recommendation.DismissedAt);
        Assert.Equal(readerId, recommendation.UpdatedBy);
    }

    [Theory]
    [InlineData("", "Title", "Description")]
    [InlineData("Module", " ", "Description")]
    [InlineData("Module", "Title", "")]
    public void Constructor_rejects_required_blank_values(string module, string title, string description)
    {
        Assert.Throws<ArgumentException>(() => new Recommendation(
            Guid.NewGuid(), module, RecommendationType.Insight,
            RecommendationSeverity.Information, title, description));
    }

    private static Recommendation CreateRecommendation() => new(
        Guid.NewGuid(),
        "Produtos",
        RecommendationType.Action,
        RecommendationSeverity.High,
        "Revisar cadastro",
        "Há informações pendentes.");
}
