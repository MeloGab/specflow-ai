namespace SpecFlowAI.Application.Abstractions;

public interface ICopilotCredentialProvider
{
    ValueTask<GitHubCopilotCredential?> GetAsync(
        CancellationToken cancellationToken = default);
}

public sealed record GitHubCopilotCredential(
    string AccessToken,
    string Login);
