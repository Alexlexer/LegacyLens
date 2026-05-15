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

Build the minimalist Vite UI before serving it from the API:

```bash
cd ui
npm install
npm run build
cd ..
dotnet run --project src/RefactorGuard.Api
```

Open the UI at the API root URL shown by `dotnet run`. Health check:

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

Generate a Markdown review report enriched with gpu-search-mcp context:

```text
POST /api/review/diff
Content-Type: application/json

{ "repoPath": "D:\\Projects\\SomeRepo" }
```

When `gpu-search-mcp` is running, the report includes dependency impact, file skeletons, and related search results for each changed file. If gpu-search-mcp is not running, the review still completes with a deterministic report and adds an `Info` finding noting the unavailability.

Two-process workflow (full enrichment):

Terminal 1 — start gpu-search-mcp:

```text
gpu-search-mcp --directory D:\Projects\SomeRepo --http --port 8765
```

Terminal 2 — start RefactorGuard:

```text
dotnet run --project src/RefactorGuard.Api
```

Generated review reports are saved to SQLite. Manage saved reports:

```text
GET /api/reports
GET /api/reports/{id}
DELETE /api/reports/{id}
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
    },
    "Persistence": {
      "DatabasePath": "data/refactorguard.db"
    }
  }
}
```
