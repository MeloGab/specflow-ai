namespace SpecFlowAI.Infrastructure.AI;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string DefaultProvider { get; init; } = "Ollama";

    public OpenAiProviderOptions OpenAI { get; init; } = new();

    public OllamaProviderOptions Ollama { get; init; } = new();

    public CopilotProviderOptions Copilot { get; init; } = new();
}

public sealed class OpenAiProviderOptions
{
    public string Model { get; init; } = "gpt-5-mini";

    public string? ApiKey { get; init; }

    public bool UseJsonSchemaResponseFormat { get; init; } = true;
}

public sealed class OllamaProviderOptions
{
    public string Model { get; init; } = "llama3.2";

    public string Endpoint { get; init; } = "http://localhost:11434";

    public int RequestTimeoutSeconds { get; init; } = 300;

    public bool UseJsonSchemaResponseFormat { get; init; } = true;
}

public sealed class CopilotProviderOptions
{
    public string Model { get; init; } = "auto";

    public int RequestTimeoutSeconds { get; init; } = 180;

    public string BaseDirectory { get; init; } =
        Path.Combine(Path.GetTempPath(), "specflow-ai-copilot");
}
