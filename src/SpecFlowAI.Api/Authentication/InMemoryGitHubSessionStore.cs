using System.Collections.Concurrent;

namespace SpecFlowAI.Api.Authentication;

public interface IGitHubSessionStore
{
    void Store(string sessionId, GitHubSession session);

    GitHubSession? Find(string sessionId);

    void Remove(string sessionId);
}

public sealed record GitHubSession(
    string AccessToken,
    string Login,
    string? AvatarUrl,
    DateTimeOffset ExpiresAtUtc);

public sealed class InMemoryGitHubSessionStore(
    TimeProvider timeProvider) : IGitHubSessionStore
{
    private readonly ConcurrentDictionary<string, GitHubSession> _sessions = new();

    public void Store(string sessionId, GitHubSession session) =>
        _sessions[sessionId] = session;

    public GitHubSession? Find(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        if (session.ExpiresAtUtc > timeProvider.GetUtcNow())
        {
            return session;
        }

        _sessions.TryRemove(sessionId, out _);
        return null;
    }

    public void Remove(string sessionId) => _sessions.TryRemove(sessionId, out _);
}
