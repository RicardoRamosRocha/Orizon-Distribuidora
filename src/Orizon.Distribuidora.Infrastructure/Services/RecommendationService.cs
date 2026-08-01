using Microsoft.EntityFrameworkCore;
using Orizon.Distribuidora.Application.Interfaces;
using Orizon.Distribuidora.Application.Recommendations;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class RecommendationService(ApplicationDbContext db) : IRecommendationService
{
    public async Task<RecommendationDto> CreateAsync(
        CreateRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var recommendation = new Recommendation(
            request.CompanyId,
            request.Module,
            request.Type,
            request.Severity,
            request.Title,
            request.Description,
            request.ReferenceId,
            request.ActionUrl,
            request.MetadataJson,
            request.ExpiresAt)
        {
            CreatedBy = request.CreatedBy
        };

        db.Recommendations.Add(recommendation);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(recommendation);
    }

    public async Task<IReadOnlyList<RecommendationDto>> GetActiveAsync(
        Guid companyId,
        string? module = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = db.Recommendations.AsNoTracking()
            .Where(item => item.CompanyId == companyId &&
                           item.DismissedAt == null &&
                           (item.ExpiresAt == null || item.ExpiresAt > now));

        if (!string.IsNullOrWhiteSpace(module))
        {
            var normalizedModule = module.Trim();
            query = query.Where(item => item.Module == normalizedModule);
        }

        var recommendations = await query
            .OrderByDescending(item => item.Severity)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return recommendations.Select(ToDto).ToList();
    }

    public async Task MarkAsReadAsync(
        Guid companyId,
        Guid recommendationId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var recommendation = await FindAsync(companyId, recommendationId, cancellationToken);
        recommendation.MarkAsRead(userId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DismissAsync(
        Guid companyId,
        Guid recommendationId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var recommendation = await FindAsync(companyId, recommendationId, cancellationToken);
        recommendation.Dismiss(userId);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Recommendation> FindAsync(
        Guid companyId,
        Guid recommendationId,
        CancellationToken cancellationToken) =>
        await db.Recommendations.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == recommendationId,
            cancellationToken)
        ?? throw new KeyNotFoundException("Recomendação não encontrada.");

    private static RecommendationDto ToDto(Recommendation item) => new(
        item.Id,
        item.CompanyId,
        item.Module,
        item.Type,
        item.Severity,
        item.Title,
        item.Description,
        item.ReferenceId,
        item.ActionUrl,
        item.MetadataJson,
        item.CreatedAt,
        item.ExpiresAt,
        item.ReadAt,
        item.DismissedAt);
}
