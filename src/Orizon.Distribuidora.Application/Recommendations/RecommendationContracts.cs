using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Application.Recommendations;

public sealed record CreateRecommendationRequest(
    Guid CompanyId,
    string Module,
    RecommendationType Type,
    RecommendationSeverity Severity,
    string Title,
    string Description,
    string? ReferenceId = null,
    string? ActionUrl = null,
    string? MetadataJson = null,
    DateTimeOffset? ExpiresAt = null,
    Guid? CreatedBy = null);

public sealed record RecommendationDto(
    Guid Id,
    Guid CompanyId,
    string Module,
    RecommendationType Type,
    RecommendationSeverity Severity,
    string Title,
    string Description,
    string? ReferenceId,
    string? ActionUrl,
    string? MetadataJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? ReadAt,
    DateTimeOffset? DismissedAt);
