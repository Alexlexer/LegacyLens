# Local Alpha Demo Checklist

End-to-end walkthrough for running LegacyLens together with `gpu-search-mcp` against a real legacy .NET repository.

> **Read-only.** LegacyLens and gpu-search-mcp never modify the repository being analysed.

---

## Architecture overview

| Component | Role |
|---|---|
| **gpu-search-mcp** | Repository search/index backend. Pattern search, semantic embedding search (sentence-transformers), dependency impact heuristics, repository signal scan, diagnostics. |
| **LegacyLens** | .NET/Roslyn-aware audit and review application. UI, report persistence, export, optional Ollama/LM Studio LLM summaries. Consumes gpu-search-mcp over HTTP. |
| **Ollama** | Optional local LLM for audit and review summaries. Used by LegacyLens only — gpu-search-mcp does not use Ollama. |

**Key distinctions:**

- Ollama is not used by gpu-search-mcp. gpu-search-mcp semantic search uses [sentence-transformers](https://www.sbert.net/) (`BAAI/bge-small-en-v1.5` by default).
- Roslyn findings are compiler-accurate facts about C# symbols.
- gpu-search findings are heuristic/retrieval-based and advisory.

---

## Prerequisites

| Requirement | Check |
|---|---|
| .NET 10 SDK | `dotnet --version` |
| Python 3.10+ with gpu-search-mcp installed | `gpu-search-mcp --help` or `.venv/Scripts/python -m gpu_service.mcp_server --help` |
| Node.js (for UI rebuild only) | `node --version` |
| Ollama (optional, for LLM summaries) | `ollama --version` |

---

## A. Clone demo repository

Use **BlogEngine.NET** — it is a small classic .NET Framework blog engine ideal for the demo.

```bash
git clone https://github.com/rxtur/BlogEngine.NET.git D:\Repos\BlogEngine.NET
```

macOS / Linux:

```bash
git clone https://github.com/rxtur/BlogEngine.NET.git ~/repos/BlogEngine.NET
```

Add the clone parent to `AllowedRoots` in `legacylens/appsettings.Local.json` (see Step F).

---

## B. Start gpu-search-mcp

Open a dedicated terminal and start gpu-search-mcp pointed at the cloned repository.

**Windows (PowerShell):**

```powershell
cd D:\Projects\gpu-search-mcp
.\.venv\Scripts\python.exe -m gpu_service.mcp_server --directory "D:\Repos\BlogEngine.NET" --http --port 8765 --device auto
```

If installed as a CLI entrypoint:

```powershell
gpu-search-mcp --directory "D:\Repos\BlogEngine.NET" --http --port 8765 --device auto
```

**macOS / Linux:**

```bash
cd ~/projects/gpu-search-mcp
python -m gpu_service.mcp_server --directory ~/repos/BlogEngine.NET --http --port 8765 --device auto
```

Wait until you see:

```
Uvicorn running on http://127.0.0.1:8765
```

`--device auto` selects CUDA (NVIDIA), MPS (Apple Silicon), or CPU automatically. The selected device is reported in `/health` and `/stats`.

---

## C. Verify gpu-search-mcp

```bash
curl http://127.0.0.1:8765/health
curl http://127.0.0.1:8765/stats
curl http://127.0.0.1:8765/diagnostics
```

Expected `/health` response:

```json
{ "ok": true, "version": "0.1.1" }
```

`/diagnostics` reports device, indexed roots, pattern/semantic/dependency readiness, cache metadata, and semantic model preflight status. It does not trigger downloads or reindexing.

---

## D. Semantic model setup (optional)

Semantic search uses **sentence-transformers**, not Ollama. The default model is `BAAI/bge-small-en-v1.5`. Pattern search and signal scan work without it.

Download the embedding model once:

```bash
gpu-search-mcp --download-semantic-model
```

Or with a specific model:

```bash
gpu-search-mcp --semantic-model BAAI/bge-small-en-v1.5 --download-semantic-model
```

After download, restart the server normally. `/stats` and `GET /semantic/model/status` show preflight status. If the model is unavailable, diagnostics report it clearly and exact pattern search continues working.

---

## E. Start Ollama (optional — for LLM summaries)

Ollama provides optional LLM executive summaries for audit and review reports. LegacyLens works without it; `useLlm: false` is the default.

```bash
ollama serve
```

Pull the recommended small model:

```bash
ollama pull gemma3:4b
```

Alternatively, use the **Ollama model** panel in the LegacyLens UI to check status and pull the configured model without leaving the browser.

---

## F. Configure LegacyLens

Copy the example local settings:

```powershell
# Windows
copy legacylens\appsettings.Local.example.json legacylens\appsettings.Local.json
```

```bash
# macOS / Linux
cp legacylens/appsettings.Local.example.json legacylens/appsettings.Local.json
```

Edit `legacylens/appsettings.Local.json` and set `AllowedRoots` to a parent of the cloned repository:

```json
{
  "LegacyLens": {
    "AllowedRoots": ["D:\\Repos"],
    "GpuSearch": {
      "BaseUrl": "http://127.0.0.1:8765"
    },
    "Review": {
      "Provider": "Ollama"
    },
    "Ollama": {
      "BaseUrl": "http://127.0.0.1:11434",
      "Model": "gemma3:4b",
      "TimeoutSeconds": 180,
      "AutoPullModel": false,
      "PullTimeoutSeconds": 600
    }
  }
}
```

`appsettings.Local.json` is git-ignored and never committed.

---

## G. Start LegacyLens

```powershell
cd D:\Projects\LegacyLens\legacylens
dotnet run --project src/LegacyLens.Api
```

macOS / Linux:

```bash
cd ~/projects/LegacyLens/legacylens
dotnet run --project src/LegacyLens.Api
```

Wait until you see `Now listening on: http://localhost:5096`, then open `http://localhost:5096` in your browser.

If the UI assets are missing or outdated, rebuild first:

```bash
cd legacylens/ui
npm ci
npm run build
cd ..
dotnet run --project src/LegacyLens.Api
```

---

## H. Run Legacy Audit

In the **Repository path** field, enter:

```
D:\Repos\BlogEngine.NET
```

Expand the **Legacy Audit** panel and configure options.

**First run — deterministic, full enrichment:**

| Option | Value |
|---|---|
| Use LLM | unchecked (`false`) |
| Include Roslyn | checked (`true`) |
| Include gpu-search | checked (`true`) |
| Include DI analysis | checked (`true`) |

Click **Run legacy audit**. The report saves automatically to SQLite and appears in the saved reports list.

**Second run — with LLM summary:**

| Option | Value |
|---|---|
| Use LLM | checked (`true`) |
| Ollama model | `gemma3:4b` |

Requires Ollama running with `gemma3:4b` pulled (see Step E).

---

## I. Export the report

1. Open **Saved reports** in the UI.
2. Click **View** on the saved Legacy Audit report.
3. Click **Export Markdown** — downloads a `.md` file.
4. Click **Export HTML** — downloads a self-contained `.html` file with embedded CSS and no CDN dependencies.

Review exported files before sharing — they may include repository paths, code snippets, findings, and optional LLM summaries.

---

## J. Expected successful demo outputs

| Check | Expected |
|---|---|
| `curl /health` | `{ "ok": true }` |
| `curl /diagnostics` | device, indexed root, pattern index ready |
| `curl /stats` | indexed file count > 0 |
| LegacyLens UI loads | `http://localhost:5096` shows UI |
| Audit summary | technology signals, risk findings, Roslyn summary, DI findings |
| Saved report appears | report visible in saved reports list immediately after audit |
| HTML export downloads | self-contained `.html` file |
| Ollama status | model installed or missing with explicit pull option |

---

## Troubleshooting

### gpu-search-mcp: semantic search unavailable

**Cause:** embedding model not downloaded locally.

**Fix:** run once to pre-download, then restart the server:

```bash
gpu-search-mcp --download-semantic-model
```

Pattern search and `/scan/signals` work without the semantic model. This is not a blocking issue for the demo.

---

### Ollama unreachable

**Fix:** start Ollama:

```bash
ollama serve
```

Deterministic audit (no LLM) still produces a full structured report without Ollama.

---

### Ollama model missing

**Fix:** pull the model:

```bash
ollama pull gemma3:4b
```

Or use the **Ollama model** panel in the LegacyLens UI to pull it without a terminal.

---

### Roslyn workspace load fails

**Cause:** BlogEngine.NET uses .NET Framework with `packages.config`. MSBuild workspace loading may fail if NuGet packages are not restored or the SDK is incompatible.

**Fix:** the audit continues with gpu-search and deterministic findings even when Roslyn fails. A `roslyn-unavailable` finding is added to the report. To attempt Roslyn: restore packages with `nuget restore` or `dotnet restore` from the repo root, then re-run the audit.

---

### LegacyLens cannot reach gpu-search-mcp

**Check:**
- `GpuSearch:BaseUrl` in `appsettings.Local.json` is `http://127.0.0.1:8765`.
- `curl http://127.0.0.1:8765/health` responds from the LegacyLens host.
- `/diagnostics` output shows the correct indexed root.

---

### Repository path outside allowed roots

LegacyLens returns `400 Bad Request`. Add the parent directory to `AllowedRoots` in `appsettings.Local.json` and restart the API.

---

### Windows execution policy for PowerShell scripts

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

---

## Related documentation

- [docs/release-readiness.md](release-readiness.md) — pre-demo validation checklist
- [docs/architecture.md](architecture.md) — layered architecture and design rules
- [docs/local-development.md](local-development.md) — dev setup and configuration reference
- gpu-search-mcp diagnostics: `GET http://127.0.0.1:8765/diagnostics`
- gpu-search-mcp signal scan: [signal-scan.md](https://github.com/Alexlexer/gpu-search-mcp/blob/master/docs/signal-scan.md)
