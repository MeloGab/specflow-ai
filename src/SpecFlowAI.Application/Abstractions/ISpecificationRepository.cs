using SpecFlowAI.Domain;

namespace SpecFlowAI.Application.Abstractions;

public interface ISpecificationRepository
{
    Task SaveAsync(SpecificationDocument document, CancellationToken cancellationToken = default);

    Task<SpecificationDocument?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
