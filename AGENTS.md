# Repository Guidelines

## Project Structure & Module Organization

LegacyLens is the planned home for **RefactorGuard**, a local AI-assisted code review and legacy .NET analysis tool powered by `gpu-search-mcp`. Use this structure as the project grows:

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
  docs/
    architecture.md
    security.md
    local-development.md
    decisions/
```

Keep endpoints thin. Put orchestration in Application, records/enums/results in Domain, and Git, `gpu-search-mcp`, SQLite, LLM, and filesystem integrations in Infrastructure.

## Build, Test, and Development Commands

No runtime code exists yet. Once the .NET solution is added, the expected root-level checks are:

- `dotnet restore`: restore project dependencies.
- `dotnet build`: compile all projects.
- `dotnet test`: run the full test suite.
- `dotnet format --verify-no-changes`: verify formatting before PRs.

If work touches Python `gpu-search-mcp`, also run available checks such as `python -m pytest`, `python -m ruff check .`, and `python -m mypy .`.

## Coding Style & Naming Conventions

Use clear C# names and keep code modular, testable, and explicit. Prefer immutable records for DTOs/results, typed options with validation, dependency injection, short single-purpose methods, and `CancellationToken` through async call chains. Avoid God services, static mutable state, hardcoded machine paths, broad exception swallowing, and hidden filesystem access.

## Testing Guidelines

Add tests with each feature. Mirror `src/` in `tests/`, and cover API validation, orchestration, infrastructure adapters, security checks, and error paths. Security-sensitive behavior, especially allowed roots, path traversal prevention, secret redaction, and read-only defaults, must be tested.

## Commit & Pull Request Guidelines

Use focused branches and conventional commits:

```text
feature/<area>-<short-description>
fix/<area>-<short-description>
docs/<area>-<short-description>

<type>(<scope>): <summary>
```

Examples: `feat(api): add git diff preview endpoint`, `docs(setup): add Windows quickstart`. Do not commit directly to `main`. Each PR must include Summary, Scope, Out of scope, Testing, Security notes, Maintenance notes, and screenshots or sample output when relevant.

## Agent-Specific Instructions

Before making changes, inspect repository state and avoid overwriting user work. Work in small PR-sized scopes, update docs with behavior changes, and verify available checks. The default security model is read-only: analysis features must not modify source repositories unless a later approved feature explicitly allows it. Never commit secrets, `.env` files, private keys, local credentials, or machine-specific absolute paths.
