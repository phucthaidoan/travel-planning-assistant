# US-01: Solution Scaffold and MAF RC4 Wiring — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a runnable .NET 10 console solution with MAF RC4 packages pinned, a minimal interactive console loop, configuration placeholders, and a Docker Compose file for PostgreSQL, Qdrant, and Jaeger.

**Architecture:** Single solution with two projects — `TravelAssistant.Console` (entry point) and `TravelAssistant.Core` (shared library). Config is loaded via `appsettings.json` + User Secrets. No agent logic in this story — just the skeleton that all future stories build on.

**Tech Stack:** .NET 10, `Microsoft.Agents.AI.OpenAI` v1.0.0-rc4, `Azure.AI.OpenAI` v2.8.0-beta.1, `Microsoft.Extensions.Configuration` v10, `Microsoft.Extensions.Hosting` v10, Docker Compose v3.8.

---

## File Map

| File | Action | Responsibility |
|------|--------|---------------|
| `global.json` | Create | Pin .NET 8 SDK |
| `TravelAssistant.sln` | Create | Solution file linking both projects |
| `src/TravelAssistant.Console/TravelAssistant.Console.csproj` | Create | Console entry point project |
| `src/TravelAssistant.Console/Program.cs` | Create | Startup: build host, start console loop |
| `src/TravelAssistant.Console/ConsoleLoop.cs` | Create | Interactive read-print loop |
| `src/TravelAssistant.Console/appsettings.json` | Create | Config placeholders (OpenAI, PostgreSQL, Qdrant, Jaeger) |
| `src/TravelAssistant.Console/appsettings.Development.json` | Create | Dev overrides (empty, ready for User Secrets) |
| `src/TravelAssistant.Core/TravelAssistant.Core.csproj` | Create | Shared library project |
| `src/TravelAssistant.Core/Configuration/AssistantOptions.cs` | Create | Strongly-typed config classes |
| `docker-compose.yml` | Create | PostgreSQL + Qdrant + Jaeger services with health checks |
| `docker-compose.override.yml` | Create | Dev port overrides |
| `.gitignore` | Create | Standard .NET gitignore |
| `tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj` | Create | xUnit test project |
| `tests/TravelAssistant.Tests/Configuration/AssistantOptionsTests.cs` | Create | Smoke-tests for config binding |

---

## Task 1: Repository skeleton and global.json

**Files:**
- Create: `global.json`
- Create: `.gitignore`

- [ ] **Step 1: Create global.json to pin the SDK**

Create `C:\Workspaces\Projects\pet\travel-planning-assistant\global.json`:

```json
{
  "sdk": {
    "version": "8.0.0",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 2: Create .gitignore**

Create `C:\Workspaces\Projects\pet\travel-planning-assistant\.gitignore`:

```
# Build outputs
bin/
obj/
*.user

# User Secrets (never commit)
secrets.json

# Rider / VS
.idea/
.vs/
*.suo
*.DotSettings.user

# Docker volumes (if mounted locally)
.docker-data/

# dotnet publish output
publish/

# NuGet
*.nupkg
.nuget/

# OS
.DS_Store
Thumbs.db
```

- [ ] **Step 3: Verify SDK is picked up**

Run from `C:\Workspaces\Projects\pet\travel-planning-assistant`:
```bash
dotnet --version
```
Expected: `8.x.x` (the pinned version or a compatible newer feature band)

---

## Task 2: Solution and project files

**Files:**
- Create: `TravelAssistant.sln`
- Create: `src/TravelAssistant.Console/TravelAssistant.Console.csproj`
- Create: `src/TravelAssistant.Core/TravelAssistant.Core.csproj`
- Create: `tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj`

- [ ] **Step 1: Create the solution and project structure**

Run from `C:\Workspaces\Projects\pet\travel-planning-assistant`:
```bash
dotnet new sln -n TravelAssistant
dotnet new console -n TravelAssistant.Console -o src/TravelAssistant.Console --framework net8.0
dotnet new classlib -n TravelAssistant.Core -o src/TravelAssistant.Core --framework net8.0
dotnet new xunit -n TravelAssistant.Tests -o tests/TravelAssistant.Tests --framework net8.0
dotnet sln add src/TravelAssistant.Console/TravelAssistant.Console.csproj
dotnet sln add src/TravelAssistant.Core/TravelAssistant.Core.csproj
dotnet sln add tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj
```

- [ ] **Step 2: Delete the generated placeholder files**

```bash
rm src/TravelAssistant.Core/Class1.cs
rm tests/TravelAssistant.Tests/UnitTest1.cs
```

- [ ] **Step 3: Add project references**

```bash
dotnet add src/TravelAssistant.Console/TravelAssistant.Console.csproj reference src/TravelAssistant.Core/TravelAssistant.Core.csproj
dotnet add tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj reference src/TravelAssistant.Core/TravelAssistant.Core.csproj
```

- [ ] **Step 4: Replace the auto-generated Console csproj with the correct one**

Replace `src/TravelAssistant.Console/TravelAssistant.Console.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>TravelAssistant.Console</RootNamespace>
    <AssemblyName>TravelAssistant.Console</AssemblyName>
    <UserSecretsId>travel-assistant-dev-secrets</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-rc4" />
    <PackageReference Include="Azure.AI.OpenAI" Version="2.8.0-beta.1" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="8.0.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\TravelAssistant.Core\TravelAssistant.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
    <None Update="appsettings.Development.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Replace the auto-generated Core csproj**

Replace `src/TravelAssistant.Core/TravelAssistant.Core.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>TravelAssistant.Core</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-rc4" />
    <PackageReference Include="Azure.AI.OpenAI" Version="2.8.0-beta.1" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.2" />
  </ItemGroup>

</Project>
```

- [ ] **Step 6: Replace the auto-generated Tests csproj**

Replace `tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <RootNamespace>TravelAssistant.Tests</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.6" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\TravelAssistant.Core\TravelAssistant.Core.csproj" />
    <ProjectReference Include="..\..\src\TravelAssistant.Console\TravelAssistant.Console.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Restore packages and verify**

```bash
dotnet restore TravelAssistant.sln
```
Expected: No errors. All packages resolve, including `Microsoft.Agents.AI.OpenAI` 1.0.0-rc4.

---

## Task 3: Strongly-typed configuration

**Files:**
- Create: `src/TravelAssistant.Core/Configuration/AssistantOptions.cs`

- [ ] **Step 1: Write the failing test first**

Create `tests/TravelAssistant.Tests/Configuration/AssistantOptionsTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TravelAssistant.Core.Configuration;

namespace TravelAssistant.Tests.Configuration;

public class AssistantOptionsTests
{
    [Fact]
    public void AssistantOptions_BindsFromConfiguration()
    {
        var configValues = new Dictionary<string, string?>
        {
            ["OpenAI:Endpoint"] = "https://my-resource.openai.azure.com/",
            ["OpenAI:ApiKey"] = "test-key",
            ["OpenAI:ChatModelId"] = "gpt-4.1-nano",
            ["OpenAI:EmbeddingModelId"] = "text-embedding-3-small",
            ["PostgreSQL:ConnectionString"] = "Host=localhost;Database=travel",
            ["Qdrant:Endpoint"] = "http://localhost:6333",
            ["Jaeger:Endpoint"] = "http://localhost:4317",
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        AssistantOptions options = new();
        config.Bind(options);

        options.OpenAI.Endpoint.Should().Be("https://my-resource.openai.azure.com/");
        options.OpenAI.ApiKey.Should().Be("test-key");
        options.OpenAI.ChatModelId.Should().Be("gpt-4.1-nano");
        options.OpenAI.EmbeddingModelId.Should().Be("text-embedding-3-small");
        options.PostgreSQL.ConnectionString.Should().Be("Host=localhost;Database=travel");
        options.Qdrant.Endpoint.Should().Be("http://localhost:6333");
        options.Jaeger.Endpoint.Should().Be("http://localhost:4317");
    }

    [Fact]
    public void AssistantOptions_HasSensibleDefaults()
    {
        AssistantOptions options = new();

        options.OpenAI.ChatModelId.Should().Be("gpt-4.1-nano");
        options.OpenAI.EmbeddingModelId.Should().Be("text-embedding-3-small");
    }
}
```

- [ ] **Step 2: Run test — expect compile failure**

```bash
dotnet test tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj --no-build 2>&1 | head -20
```
Expected: Build error — `TravelAssistant.Core.Configuration` namespace does not exist yet.

- [ ] **Step 3: Create the configuration classes**

Create `src/TravelAssistant.Core/Configuration/AssistantOptions.cs`:

```csharp
namespace TravelAssistant.Core.Configuration;

public sealed class AssistantOptions
{
    public OpenAIOptions OpenAI { get; set; } = new();
    public PostgreSQLOptions PostgreSQL { get; set; } = new();
    public QdrantOptions Qdrant { get; set; } = new();
    public JaegerOptions Jaeger { get; set; } = new();
}

public sealed class OpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModelId { get; set; } = "gpt-4.1-nano";
    public string EmbeddingModelId { get; set; } = "text-embedding-3-small";
}

public sealed class PostgreSQLOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class QdrantOptions
{
    public string Endpoint { get; set; } = "http://localhost:6333";
}

public sealed class JaegerOptions
{
    public string Endpoint { get; set; } = "http://localhost:4317";
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj -v minimal
```
Expected:
```
Passed!  - Failed: 0, Passed: 2, Skipped: 0
```

- [ ] **Step 5: Commit**

```bash
git -C "C:\Workspaces\Projects\pet\travel-planning-assistant" add -A
git -C "C:\Workspaces\Projects\pet\travel-planning-assistant" commit -m "feat(us-01): add strongly-typed configuration classes"
```

---

## Task 4: appsettings.json and appsettings.Development.json

**Files:**
- Create: `src/TravelAssistant.Console/appsettings.json`
- Create: `src/TravelAssistant.Console/appsettings.Development.json`

- [ ] **Step 1: Create appsettings.json with all placeholders**

Create `src/TravelAssistant.Console/appsettings.json`:

```json
{
  "OpenAI": {
    "Endpoint": "",
    "ApiKey": "",
    "ChatModelId": "gpt-4.1-nano",
    "EmbeddingModelId": "text-embedding-3-small"
  },
  "PostgreSQL": {
    "ConnectionString": ""
  },
  "Qdrant": {
    "Endpoint": "http://localhost:6333"
  },
  "Jaeger": {
    "Endpoint": "http://localhost:4317"
  },
  "Session": {
    "TtlDays": 30,
    "RecentMessageLimit": 10
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

- [ ] **Step 2: Create appsettings.Development.json**

Create `src/TravelAssistant.Console/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "TravelAssistant": "Debug"
    }
  }
}
```

> **Note for developers:** Sensitive values (`OpenAI:ApiKey`, `OpenAI:Endpoint`, `PostgreSQL:ConnectionString`) must be set via User Secrets:
> ```bash
> dotnet user-secrets set "OpenAI:ApiKey" "your-key" --project src/TravelAssistant.Console
> dotnet user-secrets set "OpenAI:Endpoint" "https://your-resource.openai.azure.com/" --project src/TravelAssistant.Console
> dotnet user-secrets set "PostgreSQL:ConnectionString" "Host=localhost;Port=5432;Database=travel;Username=postgres;Password=password" --project src/TravelAssistant.Console
> ```

---

## Task 5: Console entry point and interactive loop

**Files:**
- Create: `src/TravelAssistant.Console/ConsoleLoop.cs`
- Modify: `src/TravelAssistant.Console/Program.cs`

- [ ] **Step 1: Write the failing test for ConsoleLoop**

Create `tests/TravelAssistant.Tests/Console/ConsoleLoopTests.cs`:

```csharp
using FluentAssertions;
using TravelAssistant.Console;

namespace TravelAssistant.Tests.Console;

public class ConsoleLoopTests
{
    [Fact]
    public void ConsoleLoop_IsExitCommand_ReturnsTrueForExitKeywords()
    {
        ConsoleLoop.IsExitCommand("exit").Should().BeTrue();
        ConsoleLoop.IsExitCommand("EXIT").Should().BeTrue();
        ConsoleLoop.IsExitCommand("quit").Should().BeTrue();
        ConsoleLoop.IsExitCommand("q").Should().BeTrue();
    }

    [Fact]
    public void ConsoleLoop_IsExitCommand_ReturnsFalseForNormalInput()
    {
        ConsoleLoop.IsExitCommand("hello").Should().BeFalse();
        ConsoleLoop.IsExitCommand("").Should().BeFalse();
        ConsoleLoop.IsExitCommand("  ").Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test — expect compile failure**

```bash
dotnet test tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj --no-build 2>&1 | head -10
```
Expected: Build error — `TravelAssistant.Console.ConsoleLoop` does not exist.

- [ ] **Step 3: Create ConsoleLoop**

Create `src/TravelAssistant.Console/ConsoleLoop.cs`:

```csharp
namespace TravelAssistant.Console;

public sealed class ConsoleLoop
{
    public static bool IsExitCommand(string input)
    {
        string trimmed = input.Trim().ToLowerInvariant();
        return trimmed is "exit" or "quit" or "q";
    }

    public static async Task RunAsync(
        Func<string, Task<string>> handleMessage,
        CancellationToken cancellationToken = default)
    {
        PrintBanner();

        while (!cancellationToken.IsCancellationRequested)
        {
            System.Console.Write("\nYou: ");
            string? input = System.Console.ReadLine();

            if (input is null || IsExitCommand(input))
            {
                System.Console.WriteLine("\nGoodbye! Safe travels.");
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
                continue;

            try
            {
                string response = await handleMessage(input);
                System.Console.WriteLine($"\nAssistant: {response}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\n[Error] {ex.Message}");
            }
        }
    }

    private static void PrintBanner()
    {
        System.Console.WriteLine("============================================");
        System.Console.WriteLine("  Intelligent Travel Planning Assistant");
        System.Console.WriteLine("  Powered by Microsoft Agent Framework RC4");
        System.Console.WriteLine("  Type 'exit' or 'quit' to stop.");
        System.Console.WriteLine("============================================");
    }
}
```

- [ ] **Step 4: Run tests — expect pass**

```bash
dotnet test tests/TravelAssistant.Tests/TravelAssistant.Tests.csproj -v minimal
```
Expected:
```
Passed!  - Failed: 0, Passed: 4, Skipped: 0
```

- [ ] **Step 5: Update Program.cs**

Replace the generated `src/TravelAssistant.Console/Program.cs` with:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelAssistant.Console;
using TravelAssistant.Core.Configuration;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

builder.Services.Configure<AssistantOptions>(builder.Configuration);
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
});

IHost host = builder.Build();

// Placeholder handler — replaced in US-04 when OrchestratorAgent is wired
static Task<string> PlaceholderHandler(string message) =>
    Task.FromResult($"[Scaffold] Echo: {message} (agent not yet wired — US-04)");

await ConsoleLoop.RunAsync(PlaceholderHandler);
```

- [ ] **Step 6: Build and run — verify startup banner appears**

```bash
dotnet run --project src/TravelAssistant.Console/TravelAssistant.Console.csproj
```
Expected output:
```
============================================
  Intelligent Travel Planning Assistant
  Powered by Microsoft Agent Framework RC4
  Type 'exit' or 'quit' to stop.
============================================

You: _
```
Type `exit` — app exits cleanly with "Goodbye! Safe travels."

- [ ] **Step 7: Commit**

```bash
git -C "C:\Workspaces\Projects\pet\travel-planning-assistant" add -A
git -C "C:\Workspaces\Projects\pet\travel-planning-assistant" commit -m "feat(us-01): add console entry point and interactive loop"
```

---

## Task 6: Docker Compose

**Files:**
- Create: `docker-compose.yml`
- Create: `docker-compose.override.yml`

- [ ] **Step 1: Create docker-compose.yml**

Create `C:\Workspaces\Projects\pet\travel-planning-assistant\docker-compose.yml`:

```yaml
version: "3.8"

services:
  postgres:
    image: postgres:15
    container_name: travel-postgres
    environment:
      POSTGRES_DB: travel
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d travel"]
      interval: 5s
      timeout: 5s
      retries: 10

  qdrant:
    image: qdrant/qdrant:v1.9.1
    container_name: travel-qdrant
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - qdrant-data:/qdrant/storage
    healthcheck:
      test: ["CMD-SHELL", "curl -sf http://localhost:6333/readyz || exit 1"]
      interval: 5s
      timeout: 5s
      retries: 10

  jaeger:
    image: jaegertracing/all-in-one:1.57
    container_name: travel-jaeger
    environment:
      COLLECTOR_OTLP_ENABLED: "true"
    ports:
      - "16686:16686"   # Jaeger UI
      - "4317:4317"     # OTLP gRPC
      - "4318:4318"     # OTLP HTTP
    healthcheck:
      test: ["CMD-SHELL", "curl -sf http://localhost:16686/ || exit 1"]
      interval: 5s
      timeout: 5s
      retries: 10

volumes:
  postgres-data:
  qdrant-data:
```

- [ ] **Step 2: Create docker-compose.override.yml**

Create `C:\Workspaces\Projects\pet\travel-planning-assistant\docker-compose.override.yml`:

```yaml
version: "3.8"

# Development overrides — do not commit secrets here.
# Set real values via environment variables or User Secrets.
services:
  postgres:
    environment:
      POSTGRES_PASSWORD: password   # dev only
```

- [ ] **Step 3: Start services and verify health**

```bash
docker compose -f "C:\Workspaces\Projects\pet\travel-planning-assistant\docker-compose.yml" up -d
docker compose -f "C:\Workspaces\Projects\pet\travel-planning-assistant\docker-compose.yml" ps
```
Expected: All three containers show `healthy` status within ~30 seconds.

- [ ] **Step 4: Verify Jaeger UI is accessible**

Open `http://localhost:16686` in a browser — the Jaeger search UI should load.

- [ ] **Step 5: Verify Qdrant is accessible**

```bash
curl http://localhost:6333/collections
```
Expected: `{"result":{"collections":[]},"status":"ok","time":...}`

- [ ] **Step 6: Commit**

```bash
git -C "C:\Workspaces\Projects\pet\travel-planning-assistant" add docker-compose.yml docker-compose.override.yml
git -C "C:\Workspaces\Projects\pet\travel-planning-assistant" commit -m "feat(us-01): add docker-compose for postgres, qdrant, and jaeger"
```

---

## Task 7: Final build verification and AC check

- [ ] **Step 1: Full clean build**

```bash
dotnet build "C:\Workspaces\Projects\pet\travel-planning-assistant\TravelAssistant.sln" -c Release
```
Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 2: Run all tests**

```bash
dotnet test "C:\Workspaces\Projects\pet\travel-planning-assistant\TravelAssistant.sln" -v minimal
```
Expected:
```
Passed!  - Failed: 0, Passed: 4, Skipped: 0
```

- [ ] **Step 3: Verify dotnet restore from clean state**

```bash
dotnet restore "C:\Workspaces\Projects\pet\travel-planning-assistant\TravelAssistant.sln"
```
Expected: All packages resolve cleanly, `Microsoft.Agents.AI.OpenAI` 1.0.0-rc4 is listed.

- [ ] **Step 4: Final commit**

```bash
git -C "C:\Workspaces\Projects\pet\travel-planning-assistant" add -A
git -C "C:\Workspaces\Projects\pet\travel-planning-assistant" commit -m "feat(us-01): solution scaffold complete — MAF RC4 wired, console loop, docker-compose"
```

---

## Acceptance Criteria Checklist

| AC | Verified by |
|----|-------------|
| `global.json` pins `Microsoft.Agents.AI.OpenAI` v1.0.0-rc4; `dotnet restore` succeeds | Task 2 Step 7 |
| Console starts, prints banner, accepts input, exits cleanly | Task 5 Step 6 |
| `appsettings.json` contains placeholders for OpenAI, PostgreSQL, Qdrant, Jaeger | Task 4 Step 1 |
| Docker Compose defines PostgreSQL, Qdrant, Jaeger with health checks | Task 6 Step 3 |
| `dotnet build` and `dotnet run` succeed from clean clone | Task 7 Steps 1–3 |
