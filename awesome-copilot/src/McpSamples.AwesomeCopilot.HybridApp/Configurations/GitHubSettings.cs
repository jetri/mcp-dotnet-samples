namespace McpSamples.AwesomeCopilot.HybridApp.Configurations;

/// <summary>
/// This represents the GitHub repository settings for the application.
/// </summary>
/// <remarks>
/// <para>
/// These settings are automatically populated from configuration sources in this order:
/// <list type="number">
/// <item>appsettings.json (base configuration)</item>
/// <item>appsettings.{Environment}.json (environment-specific)</item>
/// <item>Environment variables (highest priority)</item>
/// <item>Command-line arguments</item>
/// </list>
/// </para>
/// <para>
/// Environment variables use double underscores (__) to represent hierarchy.
/// See individual property documentation for environment variable names.
/// </para>
/// </remarks>
public class GitHubSettings
{
    /// <summary>
    /// Gets or sets the GitHub personal access token for authentication.
    /// </summary>
    /// <remarks>
    /// <para><strong>Environment Variable:</strong> <c>GITHUB__TOKEN</c></para>
    /// <para><strong>Configuration Path:</strong> <c>GitHub:Token</c></para>
    /// <para><strong>Required:</strong> Yes - Application will fail at startup if not provided</para>
    /// <para><strong>Security:</strong> This value should NEVER be stored in appsettings.json.
    /// Always provide via environment variables or secret management systems.</para>
    /// <para>
    /// <strong>How to obtain:</strong>
    /// <list type="bullet">
    /// <item>Go to https://github.com/settings/tokens</item>
    /// <item>Generate a new personal access token</item>
    /// <item>For public repos: No special scopes needed</item>
    /// <item>For private repos: Select 'repo' scope</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub repository owner (organization or username).
    /// </summary>
    /// <remarks>
    /// <para><strong>Environment Variable:</strong> <c>GITHUB__REPOSITORYOWNER</c></para>
    /// <para><strong>Configuration Path:</strong> <c>GitHub:RepositoryOwner</c></para>
    /// <para><strong>Default:</strong> Configured in appsettings.json</para>
    /// <para><strong>Example:</strong> "github", "myorg", "myusername"</para>
    /// </remarks>
    public string RepositoryOwner { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub repository name.
    /// </summary>
    /// <remarks>
    /// <para><strong>Environment Variable:</strong> <c>GITHUB__REPOSITORYNAME</c></para>
    /// <para><strong>Configuration Path:</strong> <c>GitHub:RepositoryName</c></para>
    /// <para><strong>Default:</strong> Configured in appsettings.json</para>
    /// <para><strong>Example:</strong> "awesome-copilot", "my-private-repo"</para>
    /// </remarks>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the branch name to fetch files from.
    /// </summary>
    /// <remarks>
    /// <para><strong>Environment Variable:</strong> <c>GITHUB__BRANCH</c></para>
    /// <para><strong>Configuration Path:</strong> <c>GitHub:Branch</c></para>
    /// <para><strong>Default:</strong> "main"</para>
    /// <para><strong>Example:</strong> "main", "develop", "feature/my-branch"</para>
    /// </remarks>
    public string Branch { get; set; } = "main";
}
