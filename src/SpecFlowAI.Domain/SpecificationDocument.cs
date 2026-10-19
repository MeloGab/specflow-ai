namespace SpecFlowAI.Domain;

public sealed record SpecificationDocument(
    Guid Id,
    string BusinessDemand,
    TechnicalSpecification Specification,
    DateTimeOffset GeneratedAtUtc,
    string Generator,
    string Model,
    string SchemaVersion = "1.0");
