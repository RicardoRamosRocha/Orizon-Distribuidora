namespace Orizon.Distribuidora.Application.Interfaces;

public interface IHeaderLearningService
{
    Task<int> LearnAsync(
        Guid companyId,
        Guid? userId,
        IReadOnlyDictionary<string, string> confirmedMappings,
        CancellationToken cancellationToken = default);
}
