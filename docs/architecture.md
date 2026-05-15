# Architecture

RefactorGuard is planned as a local .NET service that reviews Git diffs and legacy .NET code with help from `gpu-search-mcp` and an LLM provider.

## Layers

```text
RefactorGuard.Api
  HTTP endpoints, request validation, response formatting

RefactorGuard.Application
  Review orchestration, workflows, interfaces, prompt construction

RefactorGuard.Domain
  Records, enums, result objects, common domain types

RefactorGuard.Infrastructure
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

`RefactorGuard.Infrastructure` provides a typed HTTP client behind `IGpuSearchClient`. The API exposes `GET /api/search/status` to check `/health` and `/stats` from `gpu-search-mcp` without coupling endpoint code to HTTP details.

## Design Rules

Keep endpoints thin, workflows testable, prompt construction isolated, and provider-specific code behind interfaces. Do not put business logic in controllers or minimal API handlers.
