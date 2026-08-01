using Orizon.Distribuidora.Application.Recommendations;

namespace Orizon.Distribuidora.Application.Interfaces;

public interface IRecommendationService
{
    Task<RecommendationDto> CreateAsync(
        CreateRecommendationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendationDto>> GetActiveAsync(
        Guid companyId,
        string? module = null,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid companyId,
        Guid recommendationId,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task DismissAsync(
        Guid companyId,
        Guid recommendationId,
        Guid? userId,
        CancellationToken cancellationToken = default);
}
