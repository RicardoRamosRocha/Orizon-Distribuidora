using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Recommendation : CompanyOwnedAuditableEntity
{
    private Recommendation()
    {
    }

    public Recommendation(
        Guid companyId,
        string module,
        RecommendationType type,
        RecommendationSeverity severity,
        string title,
        string description,
        string? referenceId = null,
        string? actionUrl = null,
        string? metadataJson = null,
        DateTimeOffset? expiresAt = null)
        : base(companyId)
    {
        Module = Required(module, nameof(module), 100);
        Title = Required(title, nameof(title), 200);
        Description = Required(description, nameof(description), 2000);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (!Enum.IsDefined(severity)) throw new ArgumentOutOfRangeException(nameof(severity));
        Type = type;
        Severity = severity;
        ReferenceId = Optional(referenceId, 200);
        ActionUrl = Optional(actionUrl, 1000);
        MetadataJson = Optional(metadataJson, 8000);
        ExpiresAt = expiresAt;
    }

    public string Module { get; private set; } = string.Empty;
    public RecommendationType Type { get; private set; }
    public RecommendationSeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceId { get; private set; }
    public string? ActionUrl { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? DismissedAt { get; private set; }

    public void MarkAsRead(Guid? userId, DateTimeOffset? occurredAt = null)
    {
        if (ReadAt.HasValue) return;
        ReadAt = occurredAt ?? DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }

    public void Dismiss(Guid? userId, DateTimeOffset? occurredAt = null)
    {
        if (DismissedAt.HasValue) return;
        DismissedAt = occurredAt ?? DateTimeOffset.UtcNow;
        UpdatedBy = userId;
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("O valor é obrigatório.", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"O valor deve ter no máximo {maxLength} caracteres.", parameterName);

        return normalized;
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"O valor deve ter no máximo {maxLength} caracteres.");
        return normalized;
    }
}
