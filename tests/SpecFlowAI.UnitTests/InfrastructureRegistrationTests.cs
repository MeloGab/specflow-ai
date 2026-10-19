using Microsoft.Extensions.DependencyInjection;
using SpecFlowAI.Application.Abstractions;
using SpecFlowAI.Infrastructure;
using SpecFlowAI.Infrastructure.AI;

namespace SpecFlowAI.UnitTests;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void AddInfrastructureRegistersOllamaWithoutApiKey()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICopilotCredentialProvider, EmptyCredentialProvider>();

        services.AddInfrastructure(new AiOptions
        {
            DefaultProvider = "Ollama",
            Ollama = new OllamaProviderOptions
            {
                Model = "llama3.2",
                Endpoint = "http://localhost:11434",
            },
        });

        var registrations = services
            .Where(service => service.ServiceType == typeof(ISpecificationGenerator))
            .ToArray();

        Assert.Equal(4, registrations.Length);
        Assert.Contains(registrations, registration =>
            registration.ImplementationType == typeof(DemoSpecificationGenerator));
        Assert.Contains(registrations, registration =>
            registration.ImplementationType == typeof(CopilotSpecificationGenerator));
        Assert.Contains(registrations, registration =>
            registration.ImplementationInstance is UnavailableSpecificationGenerator);
        Assert.Contains(registrations, registration =>
            registration.ImplementationFactory is not null);
    }

    [Fact]
    public void AddInfrastructureRejectsInvalidOllamaEndpoint()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(new AiOptions
            {
                Ollama = new OllamaProviderOptions { Endpoint = "not-a-url" },
            }));

        Assert.Contains("AI:Ollama:Endpoint", exception.Message);
    }

    [Fact]
    public void AddInfrastructureRejectsInvalidOllamaTimeout()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(new AiOptions
            {
                Ollama = new OllamaProviderOptions { RequestTimeoutSeconds = 0 },
            }));

        Assert.Contains("AI:Ollama:RequestTimeoutSeconds", exception.Message);
    }

    [Fact]
    public void AddInfrastructureRejectsUnknownProvider()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(new AiOptions { DefaultProvider = "Typo" }));

        Assert.Contains("não é suportado", exception.Message);
    }

    private sealed class EmptyCredentialProvider : ICopilotCredentialProvider
    {
        public ValueTask<GitHubCopilotCredential?> GetAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<GitHubCopilotCredential?>(null);
    }
}
