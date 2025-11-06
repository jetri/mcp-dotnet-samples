# MCP Server: Awesome Copilot

This is an MCP server that retrieves GitHub Copilot customizations from the [awesome-copilot](https://github.com/github/awesome-copilot) repository.

## Install

[![Install in VS Code](https://img.shields.io/badge/VS_Code-Install-0098FF?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/mcp/vscode) [![Install in VS Code Insiders](https://img.shields.io/badge/VS_Code_Insiders-Install-24bfa5?style=flat-square&logo=visualstudiocode&logoColor=white)](https://aka.ms/awesome-copilot/mcp/vscode-insiders)

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio Code](https://code.visualstudio.com/) with
  - [C# Dev Kit](https://marketplace.visualstudio.com/items/?itemName=ms-dotnettools.csdevkit) extension
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- [Docker Desktop](https://docs.docker.com/get-started/get-docker/)
- [GitHub Personal Access Token](https://github.com/settings/tokens) - Required for accessing the awesome-copilot repository via GitHub API

## What's Included

Awesome Copilot MCP server includes:

| Building Block | Name                  | Description                                                           | Usage                                    |
|----------------|-----------------------|-----------------------------------------------------------------------|------------------------------------------|
| Tools          | `search_instructions` | Searches custom instructions based on keywords in their descriptions. | `#search_instructions`                   |
| Tools          | `load_instruction`    | Loads a custom instruction from the repository.                       | `#load_instruction`                      |
| Prompts        | `get_search_prompt`   | Get a prompt for searching copilot instructions.                      | `/mcp.awesome-copilot.get_search_prompt` |

## Getting Started

- [Configuration](#configuration)
- [Getting repository root](#getting-repository-root)
- [Running MCP server](#running-mcp-server)
  - [On a local machine](#on-a-local-machine)
  - [In a container](#in-a-container)
  - [On Azure](#on-azure)
- [Connect MCP server to an MCP host/client](#connect-mcp-server-to-an-mcp-hostclient)
  - [VS Code + Agent Mode + Local MCP server](#vs-code--agent-mode--local-mcp-server)

### Configuration

This MCP server uses the GitHub API to fetch custom instructions from the [awesome-copilot](https://github.com/github/awesome-copilot) repository. You need to configure a GitHub Personal Access Token to authenticate with the GitHub API.

#### GitHub Personal Access Token Setup

1. Go to [GitHub Settings > Tokens](https://github.com/settings/tokens)
2. Click "Generate new token" (classic)
3. For **public repositories**: No special scopes are needed
4. For **private repositories**: Select the `repo` scope
5. Copy the generated token

#### Development Environment Setup

For local development, create a `.env` file in the `src/McpSamples.AwesomeCopilot.HybridApp` directory:

```bash
cd $REPOSITORY_ROOT/awesome-copilot/src/McpSamples.AwesomeCopilot.HybridApp
cp .env.example .env
```

Edit the `.env` file and add your GitHub token:

```bash
GITHUB__TOKEN=your_github_token_here
```

> **Security Note**: The `.env` file is automatically ignored by git. Never commit your token to source control.

#### Production Environment Setup

For Docker containers and Azure deployments, pass the token as an environment variable (see respective sections below).

#### Optional Configuration

You can override the default repository settings using environment variables:

- `GITHUB__REPOSITORYOWNER`: Repository owner (default: `github`)
- `GITHUB__REPOSITORYNAME`: Repository name (default: `awesome-copilot`)
- `GITHUB__BRANCH`: Branch name (default: `main`)

For example, to use a different repository in your `.env` file:

```bash
GITHUB__TOKEN=your_github_token_here
GITHUB__REPOSITORYOWNER=myorg
GITHUB__REPOSITORYNAME=my-repo
GITHUB__BRANCH=develop
```

### Getting repository root

1. Get the repository root.

    ```bash
    # bash/zsh
    REPOSITORY_ROOT=$(git rev-parse --show-toplevel)
    ```

    ```powershell
    # PowerShell
    $REPOSITORY_ROOT = git rev-parse --show-toplevel
    ```

### Running MCP server

#### On a local machine

1. Make sure you have configured the `.env` file with your GitHub token (see [Configuration](#configuration) section above).

1. Run the MCP server app.

    ```bash
    cd $REPOSITORY_ROOT/awesome-copilot
    dotnet run --project ./src/McpSamples.AwesomeCopilot.HybridApp
    ```

   > **Note**:
   > - The application will fail at startup if `GITHUB__TOKEN` is not set in your `.env` file
   > - Make sure to take note of the absolute directory path of the `McpSamples.AwesomeCopilot.HybridApp` project

   **Parameters**:

   - `--http`: The switch that indicates to run this MCP server as a streamable HTTP type. When this switch is added, the MCP server URL is `http://localhost:5250`.

   With this parameter, you can run the MCP server like:

   ```bash
   dotnet run --project ./src/McpSamples.AwesomeCopilot.HybridApp -- --http
   ```

#### In a container

1. Build the MCP server app as a container image.

    ```bash
    cd $REPOSITORY_ROOT
    docker build -f Dockerfile.awesome-copilot -t awesome-copilot:latest .
    ```

1. Run the MCP server app in a container.

    > **Important**: You must pass the GitHub token as an environment variable.

    ```bash
    docker run -i --rm -p 8080:8080 \
      -e GITHUB__TOKEN=your_github_token_here \
      awesome-copilot:latest
    ```

   Alternatively, use the container image from the container registry.

    ```bash
    docker run -i --rm -p 8080:8080 \
      -e GITHUB__TOKEN=your_github_token_here \
      ghcr.io/microsoft/mcp-dotnet-samples/awesome-copilot:latest
    ```

   **Parameters**:

   - `--http`: The switch that indicates to run this MCP server as a streamable HTTP type. When this switch is added, the MCP server URL is `http://localhost:8080`.
   - `-e GITHUB__TOKEN`: Environment variable for GitHub authentication (required)
   - `-e GITHUB__REPOSITORYOWNER`: Override repository owner (optional, default: `github`)
   - `-e GITHUB__REPOSITORYNAME`: Override repository name (optional, default: `awesome-copilot`)
   - `-e GITHUB__BRANCH`: Override branch name (optional, default: `main`)

   With these parameters, you can run the MCP server like:

   ```bash
   # use local container image with HTTP mode
   docker run -i --rm -p 8080:8080 \
     -e GITHUB__TOKEN=your_github_token_here \
     awesome-copilot:latest --http
   ```

   ```bash
   # use container image from the container registry with HTTP mode
   docker run -i --rm -p 8080:8080 \
     -e GITHUB__TOKEN=your_github_token_here \
     ghcr.io/microsoft/mcp-dotnet-samples/awesome-copilot:latest --http
   ```

   ```bash
   # use custom repository
   docker run -i --rm -p 8080:8080 \
     -e GITHUB__TOKEN=your_github_token_here \
     -e GITHUB__REPOSITORYOWNER=myorg \
     -e GITHUB__REPOSITORYNAME=my-repo \
     ghcr.io/microsoft/mcp-dotnet-samples/awesome-copilot:latest --http
   ```

#### On Azure

1. Navigate to the directory.

    ```bash
    cd $REPOSITORY_ROOT/awesome-copilot
    ```

1. Login to Azure.

    ```bash
    # Login with Azure Developer CLI
    azd auth login
    ```

1. Set the GitHub token as an environment variable for deployment.

    ```bash
    azd env set GITHUB__TOKEN your_github_token_here
    ```

   Optionally, configure custom repository settings:

    ```bash
    azd env set GITHUB__REPOSITORYOWNER myorg
    azd env set GITHUB__REPOSITORYNAME my-repo
    azd env set GITHUB__BRANCH develop
    ```

1. Deploy the MCP server app to Azure.

    ```bash
    azd up
    ```

   While provisioning and deploying, you'll be asked to provide subscription ID, location, environment name.

1. After the deployment is complete, get the information by running the following commands:

   - Azure Container Apps FQDN:

     ```bash
     azd env get-value AZURE_RESOURCE_MCP_AWESOME_COPILOT_FQDN
     ```

   > **Note**: The GitHub token and other environment variables are securely stored and injected into the Azure Container Apps instance. You can update them later using the Azure Portal or Azure CLI.

### Connect MCP server to an MCP host/client

#### VS Code + Agent Mode + Local MCP server

1. Copy `mcp.json` to the repository root.

   **For locally running MCP server (STDIO):**

    ```bash
    mkdir -p $REPOSITORY_ROOT/.vscode
    cp $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.stdio.local.json \
       $REPOSITORY_ROOT/.vscode/mcp.json
    ```

    ```powershell
    New-Item -Type Directory -Path $REPOSITORY_ROOT/.vscode -Force
    Copy-Item -Path $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.stdio.local.json `
              -Destination $REPOSITORY_ROOT/.vscode/mcp.json -Force
    ```

   **For locally running MCP server (HTTP):**

    ```bash
    mkdir -p $REPOSITORY_ROOT/.vscode
    cp $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.http.local.json \
       $REPOSITORY_ROOT/.vscode/mcp.json
    ```

    ```powershell
    New-Item -Type Directory -Path $REPOSITORY_ROOT/.vscode -Force
    Copy-Item -Path $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.http.local.json `
              -Destination $REPOSITORY_ROOT/.vscode/mcp.json -Force
    ```

   **For locally running MCP server in a container (STDIO):**

    ```bash
    mkdir -p $REPOSITORY_ROOT/.vscode
    cp $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.stdio.container.json \
       $REPOSITORY_ROOT/.vscode/mcp.json
    ```

    ```powershell
    New-Item -Type Directory -Path $REPOSITORY_ROOT/.vscode -Force
    Copy-Item -Path $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.stdio.container.json `
              -Destination $REPOSITORY_ROOT/.vscode/mcp.json -Force
    ```

   **For locally running MCP server in a container (HTTP):**

    ```bash
    mkdir -p $REPOSITORY_ROOT/.vscode
    cp $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.http.container.json \
       $REPOSITORY_ROOT/.vscode/mcp.json
    ```

    ```powershell
    New-Item -Type Directory -Path $REPOSITORY_ROOT/.vscode -Force
    Copy-Item -Path $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.http.container.json `
              -Destination $REPOSITORY_ROOT/.vscode/mcp.json -Force
    ```

   **For remotely running MCP server in a container (HTTP):**

    ```bash
    mkdir -p $REPOSITORY_ROOT/.vscode
    cp $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.http.remote.json \
       $REPOSITORY_ROOT/.vscode/mcp.json
    ```

    ```powershell
    New-Item -Type Directory -Path $REPOSITORY_ROOT/.vscode -Force
    Copy-Item -Path $REPOSITORY_ROOT/awesome-copilot/.vscode/mcp.http.remote.json `
              -Destination $REPOSITORY_ROOT/.vscode/mcp.json -Force
    ```

1. Open Command Palette by typing `F1` or `Ctrl`+`Shift`+`P` on Windows or `Cmd`+`Shift`+`P` on Mac OS, and search `MCP: List Servers`.
1. Choose `awesome-copilot` then click `Start Server`.
1. When prompted, enter one of the following values:
   - The absolute directory path of the `McpSamples.AwesomeCopilot.HybridApp` project
   - The FQDN of Azure Container Apps.
1. Use a prompt by typing `/mcp.awesome-copilot.get_search_prompt` and enter keywords to search. You'll get a prompt like:

    ```text
    Please search all the chatmodes, instructions and prompts that are related to the search keyword, `{keyword}`.

    Here's the process to follow:

    1. Use the `awesome-copilot` MCP server.
    1. Search all chatmodes, instructions, and prompts for the keyword provided.
    1. DO NOT load any chatmodes, instructions, or prompts from the MCP server until the user asks to do so.
    1. Scan local chatmodes, instructions, and prompts markdown files in `.github/chatmodes`, `.github/instructions`, and `.github/prompts` directories respectively.
    1. Compare existing chatmodes, instructions, and prompts with the search results.
    1. Provide a structured response in a table format that includes the already exists, mode (chatmodes, instructions or prompts), filename, title and description of each item found. Here's an example of the table format:

        | Exists | Mode         | Filename               | Title         | Description   |
        |--------|--------------|------------------------|---------------|---------------|
        | ✅    | chatmodes    | chatmode1.json         | ChatMode 1    | Description 1 |
        | ❌    | instructions | instruction1.json      | Instruction 1 | Description 1 |
        | ✅    | prompts      | prompt1.json           | Prompt 1      | Description 1 |

        ✅ indicates that the item already exists in this repository, while ❌ indicates that it does not.

    1. If any item doesn't exist in the repository, ask which item the user wants to save.
    1. If the user wants to save it, save the item in the appropriate directory (`.github/chatmodes`, `.github/instructions`, or `.github/prompts`) using the mode and filename, with NO modification.
    ```

1. Confirm the result.
