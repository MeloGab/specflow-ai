using System.Collections.Concurrent;
using SpecFlowAI.Application.Abstractions;
using SpecFlowAI.Domain;

namespace SpecFlowAI.Infrastructure.Persistence;

public sealed class InMemorySpecificationRepository : ISpecificationRepository
{
    private readonly ConcurrentDictionary<Guid, SpecificationDocument> _documents = new();

    public Task SaveAsync(
        SpecificationDocument document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _documents[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task<SpecificationDocument?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _documents.TryGetValue(id, out var document);
        return Task.FromResult(document);
    }
}
