# LegacyLens

LegacyLens is a local AI-assisted review and legacy .NET analysis tool powered by gpu-search-mcp.

## Naming note

LegacyLens is the product and repository name. Some internal .NET projects and namespaces still use the `RefactorGuard` prefix from the original bootstrap (`RefactorGuard.Api`, `RefactorGuard.Application`, etc.). These are implementation details and may be renamed in a later refactor.

## Current Status

The repository includes the full .NET solution, layered projects, test projects, CI configuration, a minimal Vite review dashboard, and SQLite report persistence.

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

## Demo workflow

See [docs/demo.md](docs/demo.md) for a full local walkthrough, including startup scripts, smoke checks, and troubleshooting.

Quick start:

```powershell
# Windows
.\scripts\start-demo.ps1 -RepoPath "D:\Projects\SomeRepo"
```

```bash
# macOS / Linux
./scripts/start-demo.sh /path/to/SomeRepo
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

Run Roslyn-based dependency injection analysis:

```text
POST /api/dotnet/di/analyze
Content-Type: application/json

{ "repoPath": "D:\\Projects\\SomeRepo" }
```

Returns `DependencyInjectionAnalysisResult` with:
- `registrations` — all detected `IServiceCollection` registrations with lifetime and type information.
- `constructorDependencies` — constructor parameters extracted from public constructors.
- `findings` — advisory findings: `multiple-registrations`, `singleton-depends-on-scoped`, `concrete-type-injection`, `missing-registration-candidate`.
- `workspacePath` / `workspaceKind` — resolved workspace.

DI analysis is static analysis only — no code is executed. Requires a `.sln`, `.slnx`, or `.csproj` discoverable from the repository root and a local .NET SDK.

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

Review reports combine three enrichment layers:

- **Roslyn reference context** — compiler-aware C# symbol references for changed `.cs` files. LegacyLens identifies the primary symbol in each changed file by filename convention, runs Roslyn reference analysis, and reports how many callers reference it across the solution. This uses the local .NET SDK/MSBuild and does not require gpu-search-mcp. If no `.sln`/`.csproj` is found or Roslyn fails, an `Info` finding is added and the review still completes.
- **gpu-search context** — when gpu-search-mcp is running: dependency impact (with confidence, analysis mode, warnings, and limitations), file skeletons, and related search results for each changed file. Dependency impact is advisory — gpu-search-mcp uses import/type/name heuristics, not a compiler.
- **Deterministic findings** — rule-based observations (large diff, config change, test change, empty diff).

If gpu-search-mcp is not running, the review still completes with deterministic + Roslyn context and adds an `Info` finding.

Two-process workflow (full enrichment):

Terminal 1 — start gpu-search-mcp:

```text
gpu-search-mcp --directory D:\Projects\SomeRepo --http --port 8765
```

Terminal 2 — start LegacyLens:

```text
dotnet run --project src/RefactorGuard.Api
```

Generated review reports are saved to SQLite. Manage saved reports:

```text
GET /api/reports
GET /api/reports/{id}
DELETE /api/reports/{id}
```

The UI report viewer renders structured sections for summary metadata, findings, gpu-search context, LM Studio summaries, and raw Markdown. It also shows confidence, warnings, limitations, related search results, skeleton previews, and copy buttons for Markdown/context/summary.

Optionally request a local LLM-enhanced summary:

```json
{ "repoPath": "D:\\Projects\\SomeRepo", "useLlm": true }
```

Configure allowed repository roots before using diff preview:

```json
{
  "LegacyLens": {
    "AllowedRoots": ["D:\\Projects"],
    "GpuSearch": {
      "BaseUrl": "http://127.0.0.1:8765",
      "TimeoutSeconds": 10
    },
    "Review": {
      "Provider": "LmStudio"
    },
    "ReviewEnrichment": {
      "MaxFilesToEnrich": 10,
      "MaxSearchResultsPerFile": 5,
      "MaxSkeletonLength": 4000,
      "MaxBlockLength": 4000,
      "MaxRelatedResultSnippetLength": 1000
    },
    "LmStudio": {
      "BaseUrl": "http://127.0.0.1:1234/v1/",
      "Model": "local-model",
      "TimeoutSeconds": 60
    },
    "Ollama": {
      "BaseUrl": "http://127.0.0.1:11434",
      "Model": "qwen2.5-coder:7b",
      "TimeoutSeconds": 120
    },
    "Persistence": {
      "DatabasePath": "data/legacylens.db"
    }
  }
}
```

The `RefactorGuard` config section is also accepted for backward compatibility.

## Review enrichment limits

gpu-search enrichment limits are configurable under `LegacyLens:ReviewEnrichment`:

- `MaxFilesToEnrich`: changed files that receive gpu-search context.
- `MaxSearchResultsPerFile`: related results requested per file.
- `MaxSkeletonLength`: maximum skeleton preview characters.
- `MaxBlockLength`: reserved cap for block-level context.
- `MaxRelatedResultSnippetLength`: maximum related result snippet characters.
- `MaxSymbolsForReferenceAnalysis`: changed C# files that receive Roslyn reference analysis (one symbol per file by filename convention).
- `MaxReferencesPerSymbol`: maximum references collected per symbol.
- `MaxTotalRoslynReferences`: cap on total Roslyn references across all symbols.

Defaults preserve existing behavior and protect prompt size/local performance. Lower values produce faster, smaller reports; higher values provide deeper context but can increase report size, token usage, and LLM summary latency.

## Ollama provider

Ollama is optional; deterministic review remains the default and `useLlm=true` is still required for an LLM summary.

```bash
ollama pull qwen2.5-coder:7b
ollama serve
```

Configure the provider:

```json
{
  "LegacyLens": {
    "Review": {
      "Provider": "Ollama"
    },
    "Ollama": {
      "BaseUrl": "http://127.0.0.1:11434",
      "Model": "qwen2.5-coder:7b",
      "TimeoutSeconds": 120
    }
  }
}
```

Prompts may contain diffs and code snippets, so use a trusted local/private Ollama instance. Ollama works well with an RTX GPU when configured to use it.
