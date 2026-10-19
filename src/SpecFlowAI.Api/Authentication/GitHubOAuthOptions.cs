namespace SpecFlowAI.Api.Authentication;

public sealed class GitHubOAuthOptions
{
    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public string FrontendUrl { get; init; } = "http://localhost:4200";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
