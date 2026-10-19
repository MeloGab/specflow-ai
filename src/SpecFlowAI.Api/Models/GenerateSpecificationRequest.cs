namespace SpecFlowAI.Api.Models;

public sealed record GenerateSpecificationRequest(
    string BusinessDemand,
    string? Provider = null);
