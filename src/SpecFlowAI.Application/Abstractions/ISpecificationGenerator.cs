using SpecFlowAI.Domain;

namespace SpecFlowAI.Application.Abstractions;

public interface ISpecificationGenerator
{
    string Provider { get; }

    Task<SpecificationGeneration> GenerateAsync(
        string businessDemand,
        CancellationToken cancellationToken = default);
}

public sealed record SpecificationGeneration(
    TechnicalSpecification Specification,
    string Generator,
    string Model);

public class SpecificationProviderException(
    string provider,
    string message,
    Exception? innerException = null) : Exception(message, innerException)
{
    public string Provider { get; } = provider;
}

public sealed class SpecificationProviderUnavailableException(
    string provider,
    string message) : SpecificationProviderException(provider, message);

public sealed class SpecificationAuthenticationRequiredException(
    string provider,
    string message) : SpecificationProviderException(provider, message);

public sealed class SpecificationProviderInvocationException(
    string provider,
    string message,
    Exception innerException) : SpecificationProviderException(provider, message, innerException);
