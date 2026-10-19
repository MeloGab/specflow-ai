using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using OpenAI.Chat;
using SpecFlowAI.Application.Abstractions;
using SpecFlowAI.Infrastructure.AI;
using SpecFlowAI.Infrastructure.Persistence;

namespace SpecFlowAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        AiOptions options)
    {
        string[] supportedProviders = ["Demo", "Ollama", "OpenAI", "GitHubCopilot"];
        if (!supportedProviders.Contains(
                options.DefaultProvider,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AI:DefaultProvider '{options.DefaultProvider}' não é suportado.");
        }

        services.AddSingleton(options);
        services.AddSingleton<ISpecificationRepository, InMemorySpecificationRepository>();
        services.AddSingleton<ISpecificationGenerator, DemoSpecificationGenerator>();

        if (string.IsNullOrWhiteSpace(options.OpenAI.ApiKey))
        {
            services.AddSingleton<ISpecificationGenerator>(
                new UnavailableSpecificationGenerator(
                    "OpenAI",
                    "Configure OPENAI_API_KEY para usar a OpenAI."));
        }
        else
        {
            services.AddSingleton<ISpecificationGenerator>(_ =>
                new AiSpecificationGenerator(
                    new ChatClient(
                        options.OpenAI.Model,
                        options.OpenAI.ApiKey).AsIChatClient(),
                    "OpenAI",
                    options.OpenAI.Model,
                    options.OpenAI.UseJsonSchemaResponseFormat));
        }

        if (!Uri.TryCreate(options.Ollama.Endpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                "AI:Ollama:Endpoint deve ser uma URL absoluta.");
        }

        if (options.Ollama.RequestTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "AI:Ollama:RequestTimeoutSeconds deve ser maior que zero.");
        }

        services.AddSingleton<ISpecificationGenerator>(_ =>
            {
                var httpClient = new HttpClient
                {
                    BaseAddress = endpoint,
                    Timeout = TimeSpan.FromSeconds(options.Ollama.RequestTimeoutSeconds),
                };

                return new AiSpecificationGenerator(
                    new OllamaApiClient(httpClient, options.Ollama.Model),
                    "Ollama",
                    options.Ollama.Model,
                    options.Ollama.UseJsonSchemaResponseFormat);
            });

        services.AddScoped<ISpecificationGenerator, CopilotSpecificationGenerator>();

        return services;
    }
}
