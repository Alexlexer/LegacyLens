# LegacyLens

LegacyLens is a local AI-assisted review and legacy .NET analysis tool powered by gpu-search-mcp.

## Naming note

LegacyLens is the product and repository name. Internal .NET projects and namespaces use the `LegacyLens` prefix throughout (`LegacyLens.Api`, `LegacyLens.Application`, etc.).

## Current Status

The repository includes the full .NET solution, layered projects, test projects, CI configuration, a minimal Vite review dashboard, and SQLite report persistence.

## Structure

```text
LegacyLens/
  src/
    LegacyLens.Api/
    LegacyLens.Application/
    LegacyLens.Domain/
    LegacyLens.Infrastructure/
  tests/
    LegacyLens.Application.Tests/
    LegacyLens.Infrastructure.Tests/
    LegacyLens.Api.Tests/
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

Run from `LegacyLens/`:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Run the API locally:

```bash
dotnet run --project src/LegacyLens.Api
```

Build the minimalist Vite UI before serving it from the API:

```bash
cd ui
npm install
npm run build
cd ..
dotnet run --project src/LegacyLens.Api
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
dotnet run --project src/LegacyLens.Api
```

Generated review reports are saved to SQLite. Manage saved reports:

```text
GET /api/reports
GET /api/reports?type=DiffReview
GET /api/reports?type=LegacyAudit
GET /api/reports/{id}
DELETE /api/reports/{id}
```

The UI report list shows both **Diff Review** and **Legacy Audit** report types with a type badge. Each saved report can be viewed or deleted. Legacy Audit reports are loaded from `GET /api/audit/reports/{id}` and rendered with the same audit panel used for live results; Diff Review reports are loaded from `GET /api/reports/{id}` and rendered in the review panel.

Saved Legacy Audit reports can be exported as local Markdown or self-contained HTML files:

```text
GET /api/audit/reports/{id}/export/markdown
GET /api/audit/reports/{id}/export/html
```

Exports are download-only and may contain repository paths, snippets, findings, and optional LLM summaries. Review exported files before sharing outside your local environment.

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

## Legacy .NET Audit Report

Generate a high-level repository audit for legacy .NET projects — useful for onboarding, modernization planning, architecture review, and risk assessment.

```text
POST /api/audit/legacy-dotnet
Content-Type: application/json

{
  "repoPath": "D:\\Projects\\SomeRepo",
  "useLlm": false,
  "includeRoslyn": true,
  "includeGpuSearch": true,
  "includeDotNetPresets": true,
  "includeDependencyInjection": true
}
```

The audit is **deterministic by default**. `useLlm: false` is the default and produces a full structured report without any LLM call. Set `useLlm: true` to add an optional local LLM executive summary.

Good candidate repositories include:

- [OrchardCMS/Orchard](https://github.com/OrchardCMS/Orchard) — ASP.NET MVC 5 CMS on .NET Framework
- [BlogEngine.NET](https://github.com/BlogEngine/BlogEngine.NET) — classic .NET Framework blog engine
- [DNN Platform](https://github.com/dnnsoftware/Dnn.Platform) — WebForms-based portal
- Old nopCommerce tags (< 4.0) — .NET Framework e-commerce

### What the audit detects

| Signal type | Examples |
|---|---|
| Technology signals | .NET Framework, ASP.NET MVC, WebForms, `packages.config`, `web.config`, `Global.asax`, SDK-style/old-style csproj |
| Architecture signals | Legacy framework, interface-driven design, multi-project solution, DI container usage |
| Risk findings | `web-config-present`, `packages-config-present`, `no-tests-detected`, `broad-exception-catch`, `sync-over-async`, `raw-sql-usage`, `service-locator-usage`, `roslyn-unavailable`, `gpu-search-unavailable` |
| DI findings | `multiple-registrations`, `singleton-depends-on-scoped`, `concrete-type-injection`, `missing-registration-candidate` |

### Enrichment layers

- **Roslyn summary** — compiler-aware when workspace loads. Reports project/document/symbol/class/interface/method counts. Falls back gracefully if no `.sln`/`.csproj` found.
- **DI analysis** — static analysis of `IServiceCollection` registration patterns and constructor dependencies. Advisory, not runtime verification.
- **gpu-search signal scan** — when gpu-search-mcp is running, LegacyLens calls `POST /scan/signals` to retrieve categorized repository signals in a single request. If `/scan/signals` is not supported (older gpu-search-mcp versions), it automatically falls back to running individual hybrid search queries (`System.Web`, `SqlConnection`, `catch (Exception)`, `.Result`, etc.) and adds an `Info` finding. All gpu-search results are heuristic/retrieval-based, not compiler-verified.
- **Recommended next steps** — deterministic, derived from detected findings.
- **LLM summary** — optional. Uses the configured local LLM provider (LM Studio or Ollama). Advisory only.

The UI includes a **Legacy Audit** panel (expandable from the main control panel) with per-option checkboxes and a "Run legacy audit" button. After each audit completes, the report is automatically saved to SQLite and appears in the saved reports list immediately. Audit reports can be reopened, deleted, or exported as Markdown/HTML from the saved reports list. The SQLite database is local and not shared.

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
