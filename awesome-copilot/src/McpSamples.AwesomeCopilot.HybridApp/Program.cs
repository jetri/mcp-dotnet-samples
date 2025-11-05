using System.Text.Json;

using DotNetEnv;

using McpSamples.AwesomeCopilot.HybridApp.Configurations;
using McpSamples.AwesomeCopilot.HybridApp.Services;
using McpSamples.AwesomeCopilot.HybridApp.Shared.Configurations;
using McpSamples.AwesomeCopilot.HybridApp.Shared.Extensions;
using McpSamples.AwesomeCopilot.HybridApp.Shared.OpenApi;

using Octokit;

// ============================================================================
// Load .env file in Development environment
// ============================================================================
// In development, we use the DotNetEnv library to load environment variables
// from a .env file. This allows developers to easily configure secrets locally
// without exposing them in source control.
//
// The .env file should be created based on .env.example and must contain:
//   GITHUB__TOKEN=your_personal_access_token
//
// In production, environment variables are injected directly into the container
// by the orchestration platform (Docker, Kubernetes, Azure App Service, etc.)
// so this .env loading is skipped.
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
    || Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development")
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
    }
}

var useStreamableHttp = AppSettings.UseStreamableHttp(Environment.GetEnvironmentVariables(), args);

// ============================================================================
// Configuration System Setup
// ============================================================================
// The builder automatically loads configuration from multiple sources in order:
//   1. appsettings.json (base configuration)
//   2. appsettings.{Environment}.json (environment-specific overrides)
//   3. Environment variables (highest priority - loaded via .env in dev)
//   4. Command-line arguments
//
// Environment Variable Naming Convention:
//   - Double underscores (__) represent hierarchy levels
//   - Example: GITHUB__TOKEN maps to configuration path "GitHub:Token"
//   - This binds to: AwesomeCopilotAppSettings.GitHub.Token
//
// Why this works:
//   - .NET's configuration system automatically converts __ to : for hierarchy
//   - The Bind() method matches keys to property names (case-insensitive)
//   - Later sources override earlier ones (env vars override appsettings.json)
IHostApplicationBuilder builder = useStreamableHttp
                                ? WebApplication.CreateBuilder(args)
                                : Host.CreateApplicationBuilder(args);

builder.Services.AddAppSettings<AwesomeCopilotAppSettings>(builder.Configuration, args);

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true
};
builder.Services.AddSingleton(options);

// ============================================================================
// GitHub API Client Configuration
// ============================================================================
// Registers the Octokit GitHubClient with authentication for GitHub API access.
//
// Configuration Mapping (automatic via .NET configuration system):
//   Environment Variable       → Configuration Path → Property
//   GITHUB__TOKEN             → GitHub:Token       → settings.GitHub.Token
//   GITHUB__REPOSITORYOWNER   → GitHub:RepositoryOwner → settings.GitHub.RepositoryOwner
//   GITHUB__REPOSITORYNAME    → GitHub:RepositoryName  → settings.GitHub.RepositoryName
//   GITHUB__BRANCH            → GitHub:Branch      → settings.GitHub.Branch
//
// Fail-Fast Validation:
//   - Validates GitHub token at startup (not on first use)
//   - Application will not start if GITHUB__TOKEN is missing or empty
//   - Prevents runtime errors and provides clear error messages
//
// Security:
//   - Token is NEVER stored in appsettings.json (only in env vars)
//   - Development: Set in .env file (not committed to source control)
//   - Production: Injected by container platform or secret management system
builder.Services.AddSingleton<IGitHubClient>(sp =>
{
    var settings = sp.GetRequiredService<AwesomeCopilotAppSettings>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    // Log configuration values (mask token for security)
    var tokenSuffix = string.IsNullOrWhiteSpace(settings.GitHub.Token)
        ? ""
        : settings.GitHub.Token.Length > 4
            ? settings.GitHub.Token[^4..]
            : new string('*', settings.GitHub.Token.Length);

    logger.LogInformation(
        "GitHub Configuration Loaded: Repository={Owner}/{Name}, Branch={Branch}, Token=***{TokenSuffix}",
        settings.GitHub.RepositoryOwner,
        settings.GitHub.RepositoryName,
        settings.GitHub.Branch,
        tokenSuffix);

    // Validate GitHub token exists (fail-fast at startup)
    if (string.IsNullOrWhiteSpace(settings.GitHub.Token))
    {
        logger.LogError("GitHub token is missing. Please set the GITHUB__TOKEN environment variable.");
        throw new InvalidOperationException(
            "GitHub token is required. Please set the GITHUB__TOKEN environment variable. " +
            "For development, create a .env file based on .env.example and add your GitHub token.");
    }

    logger.LogInformation("GitHub API client initialized successfully with authenticated credentials.");

    var client = new GitHubClient(new ProductHeaderValue("MCP-Awesome-Copilot"))
    {
        Credentials = new Credentials(settings.GitHub.Token)
    };

    return client;
});

// Register MetadataService
builder.Services.AddSingleton<IMetadataService, MetadataService>();

if (useStreamableHttp == true)
{
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddOpenApi("swagger", o =>
    {
        o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
        o.AddDocumentTransformer<McpDocumentTransformer<AwesomeCopilotAppSettings>>();
    });
    builder.Services.AddOpenApi("openapi", o =>
    {
        o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
        o.AddDocumentTransformer<McpDocumentTransformer<AwesomeCopilotAppSettings>>();
    });
}

IHost app = builder.BuildApp(useStreamableHttp);

// ============================================================================
// Force GitHub Client Initialization at Startup
// ============================================================================
// Resolve the GitHubClient immediately to trigger configuration validation
// and logging. This ensures fail-fast behavior - the app won't start if the
// GitHub token is missing or configuration is invalid.
var _ = app.Services.GetRequiredService<IGitHubClient>();

if (useStreamableHttp == true)
{
    (app as WebApplication)!.MapOpenApi("/{documentName}.json");
}

await app.RunAsync();
