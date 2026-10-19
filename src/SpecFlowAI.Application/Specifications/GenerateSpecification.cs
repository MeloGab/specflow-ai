using SpecFlowAI.Application.Abstractions;
using SpecFlowAI.Domain;

namespace SpecFlowAI.Application.Specifications;

public sealed record GenerateSpecificationCommand(
    string BusinessDemand,
    string Provider);

public sealed class GenerateSpecificationHandler
{
    private readonly Dictionary<string, ISpecificationGenerator> _generators;
    private readonly ISpecificationRepository _repository;
    private readonly TimeProvider _timeProvider;

    public const int MinimumDemandLength = 20;
    public const int MaximumDemandLength = 5_000;

    public GenerateSpecificationHandler(
        IEnumerable<ISpecificationGenerator> generators,
        ISpecificationRepository repository,
        TimeProvider timeProvider)
    {
        _generators = generators.ToDictionary(
            generator => generator.Provider,
            StringComparer.OrdinalIgnoreCase);
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<SpecificationDocument> HandleAsync(
        GenerateSpecificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var demand = command.BusinessDemand?.Trim() ?? string.Empty;

        if (demand.Length < MinimumDemandLength)
        {
            throw new SpecificationValidationException(
                $"A demanda deve ter ao menos {MinimumDemandLength} caracteres.");
        }

        if (demand.Length > MaximumDemandLength)
        {
            throw new SpecificationValidationException(
                $"A demanda deve ter no máximo {MaximumDemandLength} caracteres.");
        }

        var provider = command.Provider?.Trim() ?? string.Empty;
        if (!_generators.TryGetValue(provider, out var generator))
        {
            throw new SpecificationValidationException(
                $"O provedor '{provider}' não é suportado.");
        }

        var generation = await generator.GenerateAsync(demand, cancellationToken);
        var document = new SpecificationDocument(
            Guid.NewGuid(),
            demand,
            generation.Specification,
            _timeProvider.GetUtcNow(),
            generation.Generator,
            generation.Model);

        await _repository.SaveAsync(document, cancellationToken);
        return document;
    }
}

public sealed class SpecificationValidationException(string message) : Exception(message);
