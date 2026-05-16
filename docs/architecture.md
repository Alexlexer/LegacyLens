# Architecture

LegacyLens is a local .NET service that reviews Git diffs and legacy .NET code with help from `gpu-search-mcp` and an LLM provider.

> **Naming note:** The internal .NET projects and namespaces still use the `RefactorGuard` prefix from the original bootstrap (`RefactorGuard.Api`, `RefactorGuard.Application`, etc.). These are implementation details and may be renamed in a later refactor. The public product name is LegacyLens.

## Layers

```text
RefactorGuard.Api          (LegacyLens HTTP API)
  HTTP endpoints, request validation, response formatting

RefactorGuard.Application  (LegacyLens orchestration)
  Review orchestration, workflows, interfaces, prompt construction

RefactorGuard.Domain       (LegacyLens domain types)
  Records, enums, result objects, common domain types

RefactorGuard.Infrastructure  (LegacyLens infrastructure)
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

`RefactorGuard.Infrastructure` provides a typed HTTP client behind `IGpuSearchClient`. The LegacyLens API exposes `GET /api/search/status` to check `/health` and `/stats` from `gpu-search-mcp` without coupling endpoint code to HTTP details.

`POST /api/dotnet/analyze` uses the same client to run preset hybrid searches for common .NET review risks. Preset definitions live in Application so they remain testable and independent of HTTP details.

## Roslyn .NET Analysis Foundation

LegacyLens is beginning a compiler-aware .NET layer backed by Roslyn. The first foundation discovers `.slnx`, `.sln`, and `.csproj` workspaces, loads them through `MSBuildWorkspace` when supported by the local SDK/MSBuild installation, and extracts basic C# symbols: namespaces, types, methods, and properties.

`gpu-search-mcp` remains the fast retrieval layer for heuristic search, related code, and dependency impact. Roslyn will provide precise compiler-aware facts in later milestones. Dependency impact remains heuristic until Roslyn reference analysis is added.

The debug endpoint `POST /api/dotnet/workspace/scan` validates the repository path, reports the selected workspace candidate, and returns workspace counts plus the first 50 symbols. This endpoint is read-only and does not modify or restore project files explicitly.

## Report Persistence

`IReportRepository` is defined in Application. Infrastructure provides a SQLite implementation that stores full review reports as JSON plus indexed summary fields for listing. The review orchestrator saves each generated report.

## Frontend

The first UI is a minimal Vite app under `refactorguard/ui`. Its production build writes static assets into `RefactorGuard.Api/wwwroot`, and the LegacyLens API serves them with ASP.NET Core static file middleware.

## Design Rules

Keep endpoints thin, workflows testable, prompt construction isolated, and provider-specific code behind interfaces. Do not put business logic in controllers or minimal API handlers.
