using SpecFlowAI.Application.Abstractions;
using SpecFlowAI.Application.Specifications;
using SpecFlowAI.Domain;

namespace SpecFlowAI.UnitTests;

public sealed class GenerateSpecificationHandlerTests
{
    [Fact]
    public async Task HandleAsyncPersistsStructuredSpecification()
    {
        var repository = new RecordingRepository();
        var expectedTime = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var handler = new GenerateSpecificationHandler(
            [new StubGenerator()],
            repository,
            new FixedTimeProvider(expectedTime));

        var result = await handler.HandleAsync(new GenerateSpecificationCommand(
            "O cliente deve poder cancelar uma apólice ativa sem sinistro aberto.",
            "test-provider"));

        Assert.Equal(expectedTime, result.GeneratedAtUtc);
        Assert.Equal("test-provider", result.Generator);
        Assert.Same(result, repository.Saved);
        Assert.NotEmpty(result.Specification.AcceptanceCriteria);
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta")]
    public async Task HandleAsyncRejectsShortDemand(string demand)
    {
        var handler = new GenerateSpecificationHandler(
            [new StubGenerator()],
            new RecordingRepository(),
            TimeProvider.System);

        await Assert.ThrowsAsync<SpecificationValidationException>(
            () => handler.HandleAsync(new GenerateSpecificationCommand(
                demand,
                "test-provider")));
    }

    private sealed class StubGenerator : ISpecificationGenerator
    {
        public string Provider => "test-provider";

        public Task<SpecificationGeneration> GenerateAsync(
            string businessDemand,
            CancellationToken cancellationToken = default)
        {
            var specification = new TechnicalSpecification(
                "Resumo",
                ["Regra"],
                ["Dúvida"],
                new SuggestedApiContract("POST", "/api/test", "Teste", ["Validação"], ["200"]),
                [new AcceptanceCriterion("contexto", "ação", "resultado")],
                [new TestScenario("Cenário", "unit", "contexto", "ação", "resultado")]);

            return Task.FromResult(new SpecificationGeneration(
                specification,
                "test-provider",
                "test-model"));
        }
    }

    private sealed class RecordingRepository : ISpecificationRepository
    {
        public SpecificationDocument? Saved { get; private set; }

        public Task SaveAsync(
            SpecificationDocument document,
            CancellationToken cancellationToken = default)
        {
            Saved = document;
            return Task.CompletedTask;
        }

        public Task<SpecificationDocument?> GetAsync(
            Guid id,
            CancellationToken cancellationToken = default) => Task.FromResult(Saved);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
