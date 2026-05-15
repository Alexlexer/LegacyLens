# LegacyLens

LegacyLens contains the initial RefactorGuard implementation: a local .NET service for AI-assisted review of Git diffs and legacy .NET code.

## Current Status

The repository now includes the RefactorGuard solution skeleton, layered projects, test projects, and CI configuration. Runtime review features will be added in focused follow-up PRs.

## Structure

```text
refactorguard/
  src/
    RefactorGuard.Api/
    RefactorGuard.Application/
    RefactorGuard.Domain/
    RefactorGuard.Infrastructure/
  tests/
    RefactorGuard.Application.Tests/
    RefactorGuard.Infrastructure.Tests/
    RefactorGuard.Api.Tests/
```

## Development

Run from `refactorguard/`:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Run the API locally:

```bash
dotnet run --project src/RefactorGuard.Api
```

Health check:

```text
GET /health
```

Check `gpu-search-mcp` availability:

```text
GET /api/search/status
```

Run .NET preset analysis through `gpu-search-mcp`:

```text
POST /api/dotnet/analyze
Content-Type: application/json

{
  "repoPath": "D:\\Projects\\SomeRepo",
  "presets": ["async-blocking", "nullable-suppression"],
  "limitPerPreset": 10
}
```

If `presets` is omitted, all built-in .NET presets run.

Preview the current working-tree diff for an allowed repository:

```text
POST /api/review/diff/preview
Content-Type: application/json

{ "repoPath": "D:\\Projects\\SomeRepo" }
```

Generate a deterministic Markdown review report without an LLM:

```text
POST /api/review/diff
Content-Type: application/json

{ "repoPath": "D:\\Projects\\SomeRepo" }
```

Optionally request an LM Studio-enhanced summary:

```json
{ "repoPath": "D:\\Projects\\SomeRepo", "useLlm": true }
```

Configure allowed repository roots before using diff preview:

```json
{
  "RefactorGuard": {
    "AllowedRoots": ["D:\\Projects"],
    "GpuSearch": {
      "BaseUrl": "http://127.0.0.1:8765",
      "TimeoutSeconds": 10
    },
    "Review": {
      "Provider": "LmStudio"
    },
    "LmStudio": {
      "BaseUrl": "http://127.0.0.1:1234/v1/",
      "Model": "local-model",
      "TimeoutSeconds": 60
    }
  }
}
```
