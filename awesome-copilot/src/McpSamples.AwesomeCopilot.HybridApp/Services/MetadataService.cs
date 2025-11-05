using System.Text.Json;

using McpSamples.AwesomeCopilot.HybridApp.Configurations;
using McpSamples.AwesomeCopilot.HybridApp.Models;

using Octokit;

namespace McpSamples.AwesomeCopilot.HybridApp.Services;

/// <summary>
/// This represents the service entity for searching and loading custom instructions from the awesome-copilot repository.
/// </summary>
public class MetadataService(
    IGitHubClient githubClient,
    AwesomeCopilotAppSettings settings,
    JsonSerializerOptions options,
    ILogger<MetadataService> logger) : IMetadataService
{
    private const string MetadataFileName = "metadata.json";

    private readonly string _metadataFilePath = Path.Combine(AppContext.BaseDirectory, MetadataFileName);
    private Metadata? _cachedMetadata;

    /// <inheritdoc />
    public async Task<Metadata> SearchAsync(string keywords, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keywords) == true)
        {
            return new Metadata();
        }

        var metadata = await GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        var searchTerms = keywords.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(term => term.Trim().ToLowerInvariant())
                                  .Where(term => string.IsNullOrWhiteSpace(term) != true)
                                  .ToArray();

        logger.LogInformation("Search terms: {terms}", string.Join(", ", searchTerms));

        var result = new Metadata
        {
            // Search in ChatModes
            ChatModes = [.. metadata.ChatModes.Where(cm => ContainsAnyKeyword(cm.Title, searchTerms) == true ||
                                                           ContainsAnyKeyword(cm.Description, searchTerms) == true)],

            // Search in Instructions
            Instructions = [.. metadata.Instructions.Where(inst => ContainsAnyKeyword(inst.Title, searchTerms) == true ||
                                                                   ContainsAnyKeyword(inst.Description, searchTerms) == true)],

            // Search in Prompts
            Prompts = [.. metadata.Prompts.Where(prompt => ContainsAnyKeyword(prompt.Description, searchTerms) == true)]
        };

        return result;
    }

    /// <inheritdoc />
    public async Task<string> LoadAsync(string directory, string filename, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(directory) == true)
        {
            throw new ArgumentException("Directory cannot be null or empty", nameof(directory));
        }

        if (string.IsNullOrWhiteSpace(filename) == true)
        {
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
        }

        var githubSettings = settings.GitHub;
        var filePath = $"{directory}/{filename}";

        try
        {
            logger.LogInformation(
                "Fetching file from GitHub: {owner}/{repo}/{branch}/{path}",
                githubSettings.RepositoryOwner,
                githubSettings.RepositoryName,
                githubSettings.Branch,
                filePath);

            // Get the file content from GitHub using Octokit
            // GetAllContentsByRef returns content metadata including encoded content
            var contents = await githubClient.Repository.Content.GetAllContentsByRef(
                githubSettings.RepositoryOwner,
                githubSettings.RepositoryName,
                filePath,
                githubSettings.Branch).ConfigureAwait(false);

            if (contents == null || contents.Count == 0)
            {
                throw new NotFoundException($"File not found: {filePath}", System.Net.HttpStatusCode.NotFound);
            }

            // Get the first (and should be only) file content
            var fileContent = contents[0];
            string content;

            // Decode the content based on encoding
            if (fileContent.Encoding == "base64")
            {
                var decodedBytes = Convert.FromBase64String(fileContent.Content);
                content = System.Text.Encoding.UTF8.GetString(decodedBytes);
            }
            else
            {
                // If not base64, assume it's plain text
                content = fileContent.Content;
            }

            logger.LogInformation(
                "Successfully loaded file from GitHub: {owner}/{repo}/{branch}/{path}",
                githubSettings.RepositoryOwner,
                githubSettings.RepositoryName,
                githubSettings.Branch,
                filePath);

            return content;
        }
        catch (NotFoundException ex)
        {
            logger.LogError(
                ex,
                "File not found in GitHub repository: {owner}/{repo}/{branch}/{path}",
                githubSettings.RepositoryOwner,
                githubSettings.RepositoryName,
                githubSettings.Branch,
                filePath);

            throw new InvalidOperationException(
                $"File '{filename}' not found in directory '{directory}' at repository " +
                $"{githubSettings.RepositoryOwner}/{githubSettings.RepositoryName} (branch: {githubSettings.Branch})",
                ex);
        }
        catch (RateLimitExceededException ex)
        {
            logger.LogError(
                ex,
                "GitHub API rate limit exceeded. Reset at: {resetTime}",
                ex.Reset);

            throw new InvalidOperationException(
                $"GitHub API rate limit exceeded. Please try again after {ex.Reset.ToLocalTime():yyyy-MM-dd HH:mm:ss}",
                ex);
        }
        catch (AuthorizationException ex)
        {
            logger.LogError(
                ex,
                "GitHub authentication failed. Please check your token.");

            throw new InvalidOperationException(
                "GitHub authentication failed. Please verify your GitHub token has the required permissions.",
                ex);
        }
        catch (ApiException ex)
        {
            logger.LogError(
                ex,
                "GitHub API error while loading file: {message}",
                ex.Message);

            throw new InvalidOperationException(
                $"Failed to load file '{filename}' from GitHub: {ex.Message}",
                ex);
        }
    }

    private async Task<Metadata> GetMetadataAsync(CancellationToken cancellationToken)
    {
        if (_cachedMetadata != null)
        {
            return _cachedMetadata;
        }

        if (File.Exists(_metadataFilePath) != true)
        {
            throw new FileNotFoundException($"Metadata file not found at: {_metadataFilePath}");
        }

        var json = await File.ReadAllTextAsync(_metadataFilePath, cancellationToken).ConfigureAwait(false);
        _cachedMetadata = JsonSerializer.Deserialize<Metadata>(json, options)
                          ?? throw new InvalidOperationException("Failed to deserialize metadata");

        return _cachedMetadata;
    }

    private static bool ContainsAnyKeyword(string? text, string[] searchTerms)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var result = searchTerms.Any(term => text.Contains(term, StringComparison.InvariantCultureIgnoreCase) == true);

        return result;
    }
}
