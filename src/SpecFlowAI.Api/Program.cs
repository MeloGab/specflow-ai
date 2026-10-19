using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using SpecFlowAI.Api.Authentication;
using SpecFlowAI.Api.Models;
using SpecFlowAI.Application.Abstractions;
using SpecFlowAI.Application.Specifications;
using SpecFlowAI.Infrastructure;
using SpecFlowAI.Infrastructure.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:4200", "https://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var aiOptions = new AiOptions
{
    DefaultProvider = builder.Configuration["AI:DefaultProvider"]
        ?? builder.Configuration["AI:Provider"]
        ?? "Ollama",
    OpenAI = new OpenAiProviderOptions
    {
        Model = builder.Configuration["AI:OpenAI:Model"]
            ?? builder.Configuration["AI:Model"]
            ?? "gpt-5-mini",
        ApiKey = builder.Configuration["AI:OpenAI:ApiKey"]
            ?? builder.Configuration["AI:ApiKey"]
            ?? builder.Configuration["OPENAI_API_KEY"],
        UseJsonSchemaResponseFormat = builder.Configuration.GetValue(
            "AI:OpenAI:UseJsonSchemaResponseFormat",
            builder.Configuration.GetValue("AI:UseJsonSchemaResponseFormat", true)),
    },
    Ollama = new OllamaProviderOptions
    {
        Model = builder.Configuration["AI:Ollama:Model"] ?? "llama3.2",
        Endpoint = builder.Configuration["AI:Ollama:Endpoint"]
            ?? builder.Configuration["AI:Endpoint"]
            ?? "http://localhost:11434",
        RequestTimeoutSeconds = builder.Configuration.GetValue(
            "AI:Ollama:RequestTimeoutSeconds",
            builder.Configuration.GetValue("AI:RequestTimeoutSeconds", 300)),
        UseJsonSchemaResponseFormat = builder.Configuration.GetValue(
            "AI:Ollama:UseJsonSchemaResponseFormat",
            builder.Configuration.GetValue("AI:UseJsonSchemaResponseFormat", true)),
    },
    Copilot = new CopilotProviderOptions
    {
        Model = builder.Configuration["AI:Copilot:Model"] ?? "auto",
        RequestTimeoutSeconds = builder.Configuration.GetValue(
            "AI:Copilot:RequestTimeoutSeconds",
            180),
        BaseDirectory = builder.Configuration["AI:Copilot:BaseDirectory"]
            ?? Path.Combine(Path.GetTempPath(), "specflow-ai-copilot"),
    },
};

var gitHubOptions = new GitHubOAuthOptions
{
    ClientId = builder.Configuration["GitHubOAuth:ClientId"],
    ClientSecret = builder.Configuration["GitHubOAuth:ClientSecret"],
    FrontendUrl = builder.Configuration["GitHubOAuth:FrontendUrl"]
        ?? "http://localhost:4200",
};

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(gitHubOptions);
builder.Services.AddSingleton<IGitHubSessionStore, InMemoryGitHubSessionStore>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICopilotCredentialProvider, HttpContextCopilotCredentialProvider>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "GitHub";
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "SpecFlowAI.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
    })
    .AddOAuth("GitHub", options =>
    {
        options.ClientId = gitHubOptions.ClientId ?? "not-configured";
        options.ClientSecret = gitHubOptions.ClientSecret ?? "not-configured";
        options.CallbackPath = "/api/auth/github/callback";
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
        options.SaveTokens = false;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Scope.Clear();
        options.Scope.Add("read:user");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    context.AccessToken);
                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                request.Headers.UserAgent.ParseAdd("SpecFlowAI/1.0");

                using var response = await context.Backchannel.SendAsync(
                    request,
                    context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(
                    context.HttpContext.RequestAborted);
                using var user = await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken: context.HttpContext.RequestAborted);
                context.RunClaimActions(user.RootElement);

                var accessToken = context.AccessToken
                    ?? throw new InvalidOperationException(
                        "O GitHub não retornou um token de acesso.");
                var sessionId = Guid.NewGuid().ToString("N");
                var login = user.RootElement.GetProperty("login").GetString()
                    ?? "github-user";
                var avatarUrl = user.RootElement.TryGetProperty("avatar_url", out var avatar)
                    ? avatar.GetString()
                    : null;

                context.Identity?.AddClaim(new Claim(
                    HttpContextCopilotCredentialProvider.SessionIdClaim,
                    sessionId));

                var sessionStore = context.HttpContext.RequestServices
                    .GetRequiredService<IGitHubSessionStore>();
                var timeProvider = context.HttpContext.RequestServices
                    .GetRequiredService<TimeProvider>();
                sessionStore.Store(sessionId, new GitHubSession(
                    accessToken,
                    login,
                    avatarUrl,
                    timeProvider.GetUtcNow().AddHours(8)));
            },
            OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Redirect(
                    $"{gitHubOptions.FrontendUrl.TrimEnd('/')}?githubAuth=failed");
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddInfrastructure(aiOptions);
builder.Services.AddScoped<GenerateSpecificationHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    defaultProvider = aiOptions.DefaultProvider,
    storage = "in-memory",
}));

app.MapGet("/api/providers", (
    HttpContext context,
    IGitHubSessionStore sessionStore) =>
{
    var gitHubSession = GetGitHubSession(context, sessionStore);

    return Results.Ok(new AiProviderResponse[]
    {
        new(
            "Demo",
            "Demo",
            "Resposta determinística para demonstrar o fluxo sem consumir IA.",
            "Este servidor",
            "deterministic-template",
            Available: true,
            RequiresAuthentication: false,
            Authenticated: false,
            Account: null,
            Message: null),
        new(
            "Ollama",
            "Ollama local",
            "Modelo executado localmente, sem chave e sem cobrança por requisição.",
            "Sua máquina",
            aiOptions.Ollama.Model,
            Available: true,
            RequiresAuthentication: false,
            Authenticated: false,
            Account: null,
            Message: "O Ollama precisa estar ativo na porta configurada."),
        new(
            "OpenAI",
            "OpenAI",
            "Geração pela API da OpenAI usando a chave configurada no backend.",
            "OpenAI",
            aiOptions.OpenAI.Model,
            Available: !string.IsNullOrWhiteSpace(aiOptions.OpenAI.ApiKey),
            RequiresAuthentication: false,
            Authenticated: false,
            Account: null,
            Message: string.IsNullOrWhiteSpace(aiOptions.OpenAI.ApiKey)
                ? "Configure OPENAI_API_KEY no backend."
                : null),
        new(
            "GitHubCopilot",
            "GitHub Copilot",
            "Usa a conta e a cota do Copilot do usuário conectado.",
            "GitHub Copilot",
            aiOptions.Copilot.Model,
            Available: gitHubOptions.IsConfigured,
            RequiresAuthentication: true,
            Authenticated: gitHubSession is not null,
            Account: gitHubSession?.Login,
            Message: !gitHubOptions.IsConfigured
                ? "Configure o Client ID e o Client Secret da GitHub OAuth App."
                : gitHubSession is null
                    ? "Entre com o GitHub para usar este provedor."
                    : null),
    });
});

app.MapGet("/api/auth/github/login", () =>
{
    if (!gitHubOptions.IsConfigured)
    {
        return Results.Problem(
            title: "Login do GitHub não configurado",
            detail: "Configure GitHubOAuth:ClientId e GitHubOAuth:ClientSecret no backend.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(
        new AuthenticationProperties
        {
            RedirectUri = $"{gitHubOptions.FrontendUrl.TrimEnd('/')}?githubAuth=success",
        },
        ["GitHub"]);
});

app.MapPost("/api/auth/github/logout", async (
    HttpContext context,
    IGitHubSessionStore sessionStore) =>
{
    var sessionId = context.User.FindFirst(
        HttpContextCopilotCredentialProvider.SessionIdClaim)?.Value;
    if (!string.IsNullOrWhiteSpace(sessionId))
    {
        sessionStore.Remove(sessionId);
    }

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
});

app.MapPost("/api/specifications", async (
    GenerateSpecificationRequest request,
    GenerateSpecificationHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await handler.HandleAsync(
            new GenerateSpecificationCommand(
                request.BusinessDemand,
                request.Provider ?? aiOptions.DefaultProvider),
            cancellationToken);

        return Results.Created($"/api/specifications/{result.Id}", result);
    }
    catch (SpecificationValidationException exception)
    {
        var field = exception.Message.Contains("provedor", StringComparison.OrdinalIgnoreCase)
            ? nameof(request.Provider)
            : nameof(request.BusinessDemand);
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [exception.Message],
        });
    }
    catch (SpecificationAuthenticationRequiredException exception)
    {
        return Results.Problem(
            title: "Autenticação necessária",
            detail: exception.Message,
            statusCode: StatusCodes.Status401Unauthorized);
    }
    catch (SpecificationProviderUnavailableException exception)
    {
        return Results.Problem(
            title: "Provedor indisponível",
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (SpecificationProviderInvocationException exception)
    {
        return Results.Problem(
            title: "Falha no provedor de IA",
            detail: exception.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("GenerateSpecification")
.Produces(StatusCodes.Status201Created)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status401Unauthorized)
.ProducesProblem(StatusCodes.Status502BadGateway)
.ProducesProblem(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/api/specifications/{id:guid}", async (
    Guid id,
    ISpecificationRepository repository,
    CancellationToken cancellationToken) =>
{
    var document = await repository.GetAsync(id, cancellationToken);
    return document is null ? Results.NotFound() : Results.Ok(document);
})
.WithName("GetSpecification")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.Run();

static GitHubSession? GetGitHubSession(
    HttpContext context,
    IGitHubSessionStore sessionStore)
{
    var sessionId = context.User.FindFirst(
        HttpContextCopilotCredentialProvider.SessionIdClaim)?.Value;

    return string.IsNullOrWhiteSpace(sessionId)
        ? null
        : sessionStore.Find(sessionId);
}
