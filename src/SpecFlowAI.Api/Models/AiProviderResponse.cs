namespace SpecFlowAI.Api.Models;

public sealed record AiProviderResponse(
    string Id,
    string Name,
    string Description,
    string DataDestination,
    string Model,
    bool Available,
    bool RequiresAuthentication,
    bool Authenticated,
    string? Account,
    string? Message);
