# Architecture

LegacyLens is a local .NET service that reviews Git diffs and legacy .NET code with help from `gpu-search-mcp` and an LLM provider.

> **Naming note:** Internal .NET projects and namespaces use the `LegacyLens` prefix throughout (`LegacyLens.Api`, `LegacyLens.Application`, etc.), consistent with the public product name.

## Layers

```text
LegacyLens.Api          (LegacyLens HTTP API)
  HTTP endpoints, request validation, response formatting

LegacyLens.Application  (LegacyLens orchestration)
  Review orchestration, workflows, interfaces, prompt construction

LegacyLens.Domain       (LegacyLens domain types)
  Records, enums, result objects, common domain types

LegacyLens.Infrastructure  (LegacyLens infrastructure)
  Git CLI, gpu-search HTTP client, LLM providers, SQLite, filesystem access
```

Dependency direction is:

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
```

Application defines interfaces such as `IGitDiffService`, `IGpuSearchClient`, `IReviewLlmProvider`, and `IReportRepository`. Infrastructure implements them.

## Main Workflow

1. API receives a review request.
2. Application validates intent and coordinates the review.
3. Infrastructure reads Git diff data in read-only mode.
4. `gpu-search-mcp` finds related code and dependency impact.
5. Application builds deterministic findings or an LLM prompt.
6. Reports are returned as structured data and Markdown.

The current review workflow is deterministic and does not call an LLM. It inspects the Git diff preview and emits stable findings for empty diffs, large changes, test changes, and project/configuration changes.

When requested, the review workflow can enrich the deterministic report through `IReviewLlmProvider`. The first provider targets LM Studio's local OpenAI-compatible `chat/completions` API. LLM prompt construction is isolated behind `IReviewPromptBuilder`.

## gpu-search Integration

`LegacyLens.Infrastructure` provides a typed HTTP client behind `IGpuSearchClient`. The LegacyLens API exposes `GET /api/search/status` to check `/health` and `/stats` from `gpu-search-mcp` without coupling endpoint code to HTTP details.

`POST /api/dotnet/analyze` uses the same client to run preset hybrid searches for common .NET review risks. Preset definitions live in Application so they remain testable and independent of HTTP details.

## Roslyn .NET Analysis Foundation

LegacyLens is beginning a compiler-aware .NET layer backed by Roslyn. The foundation discovers `.slnx`, `.sln`, and `.csproj` workspaces, loads them through `MSBuildWorkspace` when supported by the local SDK/MSBuild installation, and extracts basic C# symbols: namespaces, types, methods, and properties.

Roslyn also provides C# symbol reference analysis through `SymbolFinder.FindReferencesAsync`. This is compiler-aware and .NET-specific, while `gpu-search-mcp` remains the fast broad retrieval layer for heuristic search, related code, and dependency impact.

The debug endpoint `POST /api/dotnet/workspace/scan` validates the repository path, reports the selected workspace candidate, and returns workspace counts plus the first 50 symbols. This endpoint is read-only and does not modify or restore project files explicitly.

`POST /api/dotnet/references` resolves a requested C# symbol name/full name and returns matched symbols plus source references. Review reports are not yet powered by Roslyn reference data; future work can merge Roslyn references with gpu-search context for more precise impact analysis.

## Audit Provider Pipeline

Legacy .NET Audit Reports are composed by a deterministic provider pipeline in `LegacyLens.Application`. The audit orchestrator builds a shared `AuditContext`, invokes registered `IAuditProvider` implementations in a predictable order, merges provider output, and then persists/formats the report.

Current providers cover repository technology signals, Roslyn workspace/symbol summaries, dependency injection analysis, `gpu-search-mcp` signal scans, architecture signals, and recommended next steps. Providers return data only; they do not mutate report objects directly. If one provider fails, the audit can continue with a warning/finding instead of losing the whole report.

Roslyn provides compiler-aware .NET facts, `gpu-search-mcp` provides broad repository signal retrieval, and the DI provider maps service registration analysis. Optional LLM summarization runs after deterministic provider output and does not change the provider pipeline.

## Report Persistence

`IReportRepository` is defined in Application. Infrastructure provides a SQLite implementation that stores full review reports as JSON plus indexed summary fields for listing. The review orchestrator saves each generated report.

## Frontend

The first UI is a minimal Vite app under `LegacyLens/ui`. Its production build writes static assets into `LegacyLens.Api/wwwroot`, and the LegacyLens API serves them with ASP.NET Core static file middleware.

## Design Rules

Keep endpoints thin, workflows testable, prompt construction isolated, and provider-specific code behind interfaces. Do not put business logic in controllers or minimal API handlers.
