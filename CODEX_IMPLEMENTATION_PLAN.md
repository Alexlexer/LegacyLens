# Codex Implementation Plan: RefactorGuard

## Purpose

RefactorGuard is a local AI-assisted code review and legacy .NET analysis tool powered by `gpu-search-mcp`. It orchestrates Git diff review, .NET-specific analysis, dependency impact discovery, LLM prompt construction, and Markdown report generation.

## Engineering Rules

1. Use one focused branch per feature or fix.
2. Deliver one pull request per deliverable.
3. Do not commit directly to `main`.
4. Keep `main` buildable, tested, and free of secrets or experimental code.
5. Update documentation when behavior changes.
6. Prefer small, maintainable code over clever code.
7. Default to read-only analysis unless a later approved feature explicitly changes that.

## Branch and Commit Style

Use short-lived branches:

```text
feature/<area>-<short-description>
fix/<area>-<short-description>
docs/<area>-<short-description>
test/<area>-<short-description>
chore/<area>-<short-description>
```

Use conventional commits:

```text
feat(api): add git diff preview endpoint
docs(setup): add Windows quickstart
test(security): cover path traversal rejection
```

## Architecture

Use layered .NET projects:

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
```

`Domain` has no infrastructure dependencies. Infrastructure implements interfaces defined by Application. Keep HTTP endpoints thin and delegate workflows to application services.

## Initial PR Sequence

1. Documentation and professional plan.
2. `gpu-search-mcp` HTTP mode skeleton.
3. `gpu-search-mcp` HTTP search endpoints.
4. RefactorGuard .NET solution bootstrap.
5. Git diff preview.
6. Typed `GpuSearchClient`.
7. Deterministic review workflow.
8. LLM abstraction and Ollama provider.
9. .NET analysis presets.
10. Report persistence.
11. Minimal frontend.

## Required Checks

For .NET work:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

For Python `gpu-search-mcp` work, run available checks:

```bash
python -m pytest
python -m ruff check .
python -m mypy .
```

If a check is unavailable, document why in the PR.
