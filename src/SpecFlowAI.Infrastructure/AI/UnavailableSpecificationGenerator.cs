using SpecFlowAI.Application.Abstractions;

namespace SpecFlowAI.Infrastructure.AI;

public sealed class UnavailableSpecificationGenerator(
    string provider,
    string reason) : ISpecificationGenerator
{
    public string Provider => provider;

    public Task<SpecificationGeneration> GenerateAsync(
        string businessDemand,
        CancellationToken cancellationToken = default) =>
        throw new SpecificationProviderUnavailableException(provider, reason);
}
