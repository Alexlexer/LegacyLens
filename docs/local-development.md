# Local Development

## Target Setup

Primary workstation:

- Windows PC
- Intel i5 13th gen
- 32 GB RAM
- RTX 4060 8 GB
- `gpu-search-mcp`
- RefactorGuard API
- local LLM runtime or configured external LLM client

Secondary client:

- MacBook Air M4
- Browser, VS Code, documentation, and remote client workflows

## Expected Commands

After the .NET solution exists, run these from the repository root:

```bash
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

If Python `gpu-search-mcp` is changed, also run available Python checks:

```bash
python -m pytest
python -m ruff check .
python -m mypy .
```

## Development Flow

1. Read `AGENTS.md` and `CODEX_IMPLEMENTATION_PLAN.md`.
2. Create a focused branch.
3. Implement only the requested scope.
4. Add or update tests.
5. Update docs.
6. Run checks.
7. Commit with a conventional commit message.
8. Push the branch and open a PR.
