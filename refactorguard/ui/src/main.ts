import './styles.css';

type SearchStatus = {
  isAvailable: boolean;
  health?: { status: string } | null;
  stats?: {
    status: string;
    backend?: string | null;
    device?: string | null;
    indexedFileCount?: number | null;
  } | null;
  error?: string | null;
};

type GitDiffFile = {
  path: string;
  status: string;
  additions: number;
  deletions: number;
};

type DiffPreview = {
  changedFileCount: number;
  files: GitDiffFile[];
  diff: string;
};

type ReviewReport = {
  reportId: string;
  repoPath: string;
  changedFileCount: number;
  markdown: string;
  llmProvider: string;
};

type ReportSummary = {
  reportId: string;
  repoPath: string;
  generatedAtUtc: string;
  changedFileCount: number;
  llmProvider: string;
};

type DotNetFinding = {
  presetId: string;
  severity: string;
  filePath: string;
  line?: number | null;
  snippet: string;
  rationale: string;
};

type DotNetAnalysis = {
  findings: DotNetFinding[];
};

const app = document.querySelector<HTMLDivElement>('#app');

if (!app) {
  throw new Error('App root was not found.');
}

app.innerHTML = `
  <main class="shell">
    <section class="hero panel">
      <div>
        <p class="eyebrow">Local legacy .NET review</p>
        <h1>LegacyLens</h1>
        <p class="muted">Flat, local-first review workflow powered by Git diffs, gpu-search-mcp, and optional LM Studio summaries.</p>
      </div>
      <button id="statusButton" class="ghost">Check gpu-search</button>
    </section>

    <section class="grid">
      <div class="panel stack">
        <label for="repoPath">Repository path</label>
        <input id="repoPath" placeholder="D:\\\\Projects\\\\ExampleRepo" />
        <label class="check"><input id="useLlm" type="checkbox" /> Include LM Studio summary</label>
        <div class="actions">
          <button id="previewButton">Preview diff</button>
          <button id="reviewButton">Run review</button>
          <button id="analyzeButton" class="secondary">.NET analysis</button>
        </div>
      </div>
      <div class="panel stack">
        <div class="row">
          <h2>Saved reports</h2>
          <button id="refreshReportsButton" class="ghost small">Refresh</button>
        </div>
        <div id="reports" class="list muted">No reports loaded.</div>
      </div>
    </section>

    <section class="panel stack">
      <div class="row">
        <h2>Output</h2>
        <span id="statusPill" class="pill">Ready</span>
      </div>
      <div id="output" class="output muted">Run a preview, review, or analysis to see results.</div>
    </section>
  </main>
`;

const repoPath = getInput('repoPath');
const useLlm = getInput('useLlm') as HTMLInputElement;
const output = getElement('output');
const statusPill = getElement('statusPill');
const reports = getElement('reports');

bind('statusButton', checkStatus);
bind('previewButton', previewDiff);
bind('reviewButton', runReview);
bind('analyzeButton', runAnalysis);
bind('refreshReportsButton', loadReports);

void loadReports();

async function checkStatus(): Promise<void> {
  await run('Checking gpu-search...', async () => {
    const status = await api<SearchStatus>('/api/search/status');
    output.innerHTML = card(
      status.isAvailable ? 'gpu-search is available' : 'gpu-search is unavailable',
      [
        `Health: ${status.health?.status ?? 'n/a'}`,
        `Backend: ${status.stats?.backend ?? 'n/a'}`,
        `Device: ${status.stats?.device ?? 'n/a'}`,
        `Indexed files: ${status.stats?.indexedFileCount ?? 'n/a'}`,
        status.error ? `Error: ${status.error}` : '',
      ].filter(Boolean),
    );
  });
}

async function previewDiff(): Promise<void> {
  await run('Loading diff preview...', async () => {
    const preview = await api<DiffPreview>('/api/review/diff/preview', {
      method: 'POST',
      body: JSON.stringify({ repoPath: repoPath.value }),
    });
    output.innerHTML = `
      ${card(`${preview.changedFileCount} changed file(s)`, preview.files.map(formatFile))}
      <pre>${escapeHtml(preview.diff || 'No diff content.')}</pre>
    `;
  });
}

async function runReview(): Promise<void> {
  await run('Running review...', async () => {
    const report = await api<ReviewReport>('/api/review/diff', {
      method: 'POST',
      body: JSON.stringify({ repoPath: repoPath.value, useLlm: useLlm.checked }),
    });
    output.innerHTML = `
      ${card(`Saved report ${report.reportId}`, [
        `Repository: ${report.repoPath}`,
        `Files: ${report.changedFileCount}`,
        `Provider: ${report.llmProvider}`,
      ])}
      <pre>${escapeHtml(report.markdown)}</pre>
    `;
    await loadReports();
  });
}

async function runAnalysis(): Promise<void> {
  await run('Running .NET analysis...', async () => {
    const analysis = await api<DotNetAnalysis>('/api/dotnet/analyze', {
      method: 'POST',
      body: JSON.stringify({ repoPath: repoPath.value, limitPerPreset: 8 }),
    });
    output.innerHTML = card(
      `${analysis.findings.length} preset finding(s)`,
      analysis.findings.map((finding) =>
        `${finding.presetId} · ${finding.filePath}${finding.line ? `:${finding.line}` : ''} · ${finding.snippet}`,
      ),
    );
  });
}

async function loadReports(): Promise<void> {
  try {
    const items = await api<ReportSummary[]>('/api/reports');
    reports.innerHTML = items.length
      ? items.map(reportItem).join('')
      : '<p class="muted">No saved reports yet.</p>';
    reports.querySelectorAll<HTMLButtonElement>('[data-view]').forEach((button) => {
      button.addEventListener('click', () => viewReport(button.dataset.view ?? ''));
    });
    reports.querySelectorAll<HTMLButtonElement>('[data-delete]').forEach((button) => {
      button.addEventListener('click', () => deleteReport(button.dataset.delete ?? ''));
    });
  } catch (error) {
    reports.innerHTML = `<p class="error">${escapeHtml(String(error))}</p>`;
  }
}

async function viewReport(id: string): Promise<void> {
  await run('Loading report...', async () => {
    const report = await api<ReviewReport>(`/api/reports/${encodeURIComponent(id)}`);
    output.innerHTML = `<pre>${escapeHtml(report.markdown)}</pre>`;
  });
}

async function deleteReport(id: string): Promise<void> {
  await run('Deleting report...', async () => {
    await api(`/api/reports/${encodeURIComponent(id)}`, { method: 'DELETE' });
    await loadReports();
    output.innerHTML = '<p class="muted">Report deleted.</p>';
  });
}

async function run(label: string, action: () => Promise<void>): Promise<void> {
  setStatus(label);
  try {
    await action();
    setStatus('Ready');
  } catch (error) {
    setStatus('Error');
    output.innerHTML = `<p class="error">${escapeHtml(String(error))}</p>`;
  }
}

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    ...init,
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function reportItem(report: ReportSummary): string {
  return `
    <article class="report">
      <div>
        <strong>${escapeHtml(report.repoPath)}</strong>
        <span>${new Date(report.generatedAtUtc).toLocaleString()} · ${report.changedFileCount} files · ${report.llmProvider}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${escapeHtml(report.reportId)}">View</button>
        <button class="danger small" data-delete="${escapeHtml(report.reportId)}">Delete</button>
      </div>
    </article>
  `;
}

function card(title: string, rows: string[]): string {
  return `
    <div class="result-card">
      <h3>${escapeHtml(title)}</h3>
      <ul>${rows.map((row) => `<li>${escapeHtml(row)}</li>`).join('')}</ul>
    </div>
  `;
}

function formatFile(file: GitDiffFile): string {
  return `${file.status} ${file.path} (+${file.additions}/-${file.deletions})`;
}

function bind(id: string, handler: () => void | Promise<void>): void {
  getElement(id).addEventListener('click', () => void handler());
}

function getInput(id: string): HTMLInputElement {
  const element = document.getElementById(id);
  if (!(element instanceof HTMLInputElement)) {
    throw new Error(`${id} input was not found.`);
  }

  return element;
}

function getElement(id: string): HTMLElement {
  const element = document.getElementById(id);
  if (!element) {
    throw new Error(`${id} element was not found.`);
  }

  return element;
}

function setStatus(value: string): void {
  statusPill.textContent = value;
}

function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#039;');
}
