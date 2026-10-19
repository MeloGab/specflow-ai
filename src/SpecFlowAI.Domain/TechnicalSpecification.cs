namespace SpecFlowAI.Domain;

public sealed record TechnicalSpecification(
    string Summary,
    IReadOnlyList<string> BusinessRules,
    IReadOnlyList<string> OpenQuestions,
    SuggestedApiContract SuggestedApi,
    IReadOnlyList<AcceptanceCriterion> AcceptanceCriteria,
    IReadOnlyList<TestScenario> TestScenarios);

public sealed record SuggestedApiContract(
    string Method,
    string Route,
    string Purpose,
    IReadOnlyList<string> Validations,
    IReadOnlyList<string> Responses);

public sealed record AcceptanceCriterion(
    string Given,
    string When,
    string Then);

public sealed record TestScenario(
    string Name,
    string Type,
    string Given,
    string When,
    string Then);
