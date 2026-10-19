using SpecFlowAI.Application.Abstractions;

namespace SpecFlowAI.Api.Authentication;

public sealed class HttpContextCopilotCredentialProvider(
    IHttpContextAccessor httpContextAccessor,
    IGitHubSessionStore sessionStore) : ICopilotCredentialProvider
{
    public const string SessionIdClaim = "specflow:github-session-id";

    public ValueTask<GitHubCopilotCredential?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sessionId = httpContextAccessor.HttpContext?.User
            .FindFirst(SessionIdClaim)?.Value;
        var session = string.IsNullOrWhiteSpace(sessionId)
            ? null
            : sessionStore.Find(sessionId);

        return ValueTask.FromResult(session is null
            ? null
            : new GitHubCopilotCredential(session.AccessToken, session.Login));
    }
}
