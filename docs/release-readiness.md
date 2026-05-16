# Release Readiness Checklist — LegacyLens

Pre-release validation pass for a local alpha demo or stable release cut.

---

## 1. Tests

```bash
cd legacylens
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

All tests must pass with zero failures or errors.

---

## 2. Code format

```bash
cd legacylens
dotnet format --verify-no-changes
```

Must exit 0 with no formatting differences reported.

---

## 3. UI build

```bash
cd legacylens/ui
npm ci
npm run build
```

Build must complete without errors. The output lands in `src/LegacyLens.Api/wwwroot/`.

---

## 4. Demo run — BlogEngine.NET

Follow [docs/demo-alpha.md](demo-alpha.md) steps A through J using BlogEngine.NET as the target:

```bash
git clone https://github.com/rxtur/BlogEngine.NET.git D:\Repos\BlogEngine.NET
```

Verify:

- [ ] gpu-search-mcp starts and `/health` responds.
- [ ] LegacyLens UI loads at `http://localhost:5096`.
- [ ] Legacy Audit (deterministic, no LLM) completes without error.
- [ ] Audit report saved and visible in saved reports list.
- [ ] **Export Markdown** downloads a `.md` file.
- [ ] **Export HTML** downloads a self-contained `.html` file.

---

## 5. Verify no secrets in exported report

Open the downloaded Markdown and HTML files and confirm:

- [ ] No API keys, bearer tokens, or passwords.
- [ ] No database connection strings.
- [ ] No private key material.
- [ ] Repository paths are local paths only (expected; acceptable for local use).

gpu-search-mcp applies best-effort redaction to search output. Do not rely on this as a guarantee — verify manually before sharing exports externally.

---

## 6. Verify local data is git-ignored

```bash
cd legacylens
git status
```

Confirm the following are not staged or tracked:

- [ ] `data/legacylens.db` (SQLite report database)
- [ ] `appsettings.Local.json`
- [ ] Any `.env` files

Check `.gitignore` includes `data/`, `*.db`, and `appsettings.Local.json`.

---

## 7. README quickstart

Review `README.md` and confirm:

- [ ] Demo workflow link points to `docs/demo-alpha.md`.
- [ ] Release readiness link points to `docs/release-readiness.md`.
- [ ] `dotnet run --project src/LegacyLens.Api` command works from `legacylens/`.
- [ ] Ollama section describes correct provider config.
- [ ] gpu-search-mcp integration section describes `BaseUrl` and `/diagnostics`.

---

## 8. Known limitations

Document before release:

- **Roslyn workspace loading** may fail for old .NET Framework repos with `packages.config` if NuGet packages are not restored. Audit continues with gpu-search and deterministic findings; a `roslyn-unavailable` finding is added.
- **DI analysis** is static analysis of `IServiceCollection` patterns — not runtime container verification. Findings are advisory.
- **gpu-search findings** are heuristic/retrieval-based, not compiler-verified. Roslyn findings are compiler-accurate.
- **LLM summaries** are optional and require Ollama or LM Studio running locally. Deterministic mode is the default and does not require an LLM.
- **Semantic search** in gpu-search-mcp requires the sentence-transformers embedding model to be downloaded once. Pattern search and signal scan work without it.
- **Export files** may contain repository paths and code snippets — review before sharing outside a local environment.
- The SQLite database (`data/legacylens.db`) is local and not shared. It is safe to delete.

---

## Related

- [docs/demo-alpha.md](demo-alpha.md) — full local demo walkthrough
- [gpu-search-mcp release-readiness](https://github.com/Alexlexer/gpu-search-mcp/blob/master/docs/release-readiness.md)
