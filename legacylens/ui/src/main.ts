import './styles.css';

// ============================================================
// Types
// ============================================================

type SearchStatus = {
  isAvailable: boolean;
  health?: { status?: string } | null;
  stats?: {
    status?: string;
    backend?: string | null;
    device?: string | null;
    indexedFileCount?: number | null;
  } | null;
  error?: string | null;
};

type OllamaModelStatus = {
  serverReachable?: boolean;
  baseUrl?: string;
  configuredModel?: string;
  modelInstalled?: boolean;
  installedModels?: string[];
  message?: string;
  error?: string | null;
};


type GitDiffFile = {
  path?: string;
  filePath?: string;
  status?: string;
  additions?: number;
  deletions?: number;
};

type DiffPreview = {
  changedFileCount?: number;
  files?: GitDiffFile[];
  diff?: string;
};

type ReviewFinding = {
  ruleId?: string;
  severity?: string;
  path?: string | null;
  filePath?: string | null;
  line?: number | null;
  lineStart?: number | null;
  lineEnd?: number | null;
  category?: string | null;
  title?: string | null;
  message?: string | null;
  description?: string | null;
};

type DependencyImpactSummary = {
  totalImpacted?: number | null;
  directImporters?: string[] | null;
  impactedFiles?: ImpactedFile[] | string[] | null;
  confidence?: string | null;
  analysisMode?: string | null;
  warnings?: string[] | null;
  limitations?: string[] | null;
  summary?: string | null;
};

type ImpactedFile = {
  file?: string;
  filePath?: string;
  path?: string;
  hops?: number | null;
  reason?: string | null;
};

type RelatedCodeResult = {
  file?: string;
  filePath?: string;
  line?: number | null;
  lineStart?: number | null;
  lineEnd?: number | null;
  score?: number | null;
  reason?: string | null;
  engine?: string | null;
  snippet?: string | null;
};

type SkeletonSummary = {
  content?: string | null;
  language?: string | null;
};

type ChangedFileContext = {
  filePath?: string;
  path?: string;
  dependencyImpact?: DependencyImpactSummary | null;
  confidence?: string | null;
  analysisMode?: string | null;
  impactedFiles?: ImpactedFile[] | string[] | null;
  warnings?: string[] | null;
  limitations?: string[] | null;
  relatedResults?: RelatedCodeResult[] | null;
  skeleton?: SkeletonSummary | string | null;
  error?: string | null;
};

type GpuSearchContext = {
  wasAvailable?: boolean;
  unavailableReason?: string | null;
  files?: ChangedFileContext[] | null;
  warnings?: string[] | null;
  limitations?: string[] | null;
};

type ChangedSymbolSummary = {
  name?: string;
  fullName?: string;
  kind?: string;
  filePath?: string;
  line?: number;
  column?: number;
  projectName?: string;
};

type RoslynReferenceSummary = {
  symbolName?: string;
  symbolFullName?: string;
  symbolKind?: string;
  filePath?: string;
  line?: number;
  column?: number;
  projectName?: string;
  containingSymbol?: string | null;
  referenceKind?: string;
  isDefinition?: boolean;
};

type RoslynReviewContext = {
  success?: boolean;
  workspacePath?: string | null;
  workspaceKind?: string | null;
  changedSymbols?: ChangedSymbolSummary[] | null;
  symbolReferences?: RoslynReferenceSummary[] | null;
  warnings?: string[] | null;
  errorMessage?: string | null;
};

type ReviewReport = {
  id?: string;
  reportId?: string;
  repoPath?: string;
  createdAtUtc?: string;
  createdAt?: string;
  generatedAtUtc?: string;
  changedFileCount?: number;
  files?: GitDiffFile[];
  findings?: ReviewFinding[];
  markdown?: string;
  llmSummary?: string | null;
  providerName?: string | null;
  llmProvider?: string | null;
  reviewMode?: string | null;
  analysisMode?: string | null;
  gpuSearchContext?: GpuSearchContext | null;
  roslynContext?: RoslynReviewContext | null;
};

type ReportSummary = {
  reportId?: string;
  id?: string;
  repoPath?: string;
  generatedAtUtc?: string;
  createdAtUtc?: string;
  changedFileCount?: number;
  findingCount?: number;
  llmProvider?: string;
  providerName?: string;
  reportType?: string;
  title?: string | null;
};

type DotNetFinding = {
  presetId?: string;
  severity?: string;
  filePath?: string;
  line?: number | null;
  snippet?: string;
  rationale?: string;
};

type DotNetAnalysis = {
  findings?: DotNetFinding[];
};

type AuditFinding = {
  severity?: string;
  code?: string;
  title?: string;
  message?: string;
  filePath?: string | null;
  line?: number | null;
  evidence?: string | null;
};

type TechnologySignal = {
  name?: string;
  category?: string;
  evidence?: string;
  filePath?: string | null;
  confidence?: string;
};

type ArchitectureSignal = {
  name?: string;
  message?: string;
  evidence?: string;
  confidence?: string;
};

type AuditRoslynSummary = {
  workspaceLoaded?: boolean;
  workspacePath?: string | null;
  workspaceKind?: string | null;
  projectCount?: number;
  documentCount?: number;
  symbolCount?: number;
  classCount?: number;
  interfaceCount?: number;
  methodCount?: number;
  warnings?: string[] | null;
  errorMessage?: string | null;
};

type DiSummary = {
  registrationCount?: number;
  constructorDependencyCount?: number;
  findingCount?: number;
  registrationsByLifetime?: Record<string, number> | null;
  findings?: AuditFinding[] | null;
};

type GpuSearchAuditResult = {
  query?: string;
  filePath?: string | null;
  line?: number | null;
  snippet?: string | null;
};

type GpuSearchAuditSummary = {
  wasAvailable?: boolean;
  queriesRun?: number;
  totalResults?: number;
  results?: GpuSearchAuditResult[] | null;
  errorMessage?: string | null;
  usedSignalScan?: boolean;
  signalCategories?: string[] | null;
  scanLimitations?: string[] | null;
  scanWarnings?: string[] | null;
};

type LegacyAuditReport = {
  reportId?: string;
  repoPath?: string;
  generatedAtUtc?: string;
  summary?: string;
  workspaceSummary?: {
    selectedWorkspacePath?: string | null;
    selectedWorkspaceKind?: string | null;
    totalCandidates?: number;
    slnxCount?: number;
    slnCount?: number;
    csprojCount?: number;
    warnings?: string[] | null;
  } | null;
  technologySignals?: TechnologySignal[] | null;
  architectureSignals?: ArchitectureSignal[] | null;
  riskFindings?: AuditFinding[] | null;
  roslynSummary?: AuditRoslynSummary | null;
  dependencyInjectionSummary?: DiSummary | null;
  gpuSearchSummary?: GpuSearchAuditSummary | null;
  recommendedNextSteps?: string[] | null;
  llmSummary?: string | null;
  markdown?: string;
  metrics?: {
    technologySignals?: number;
    riskFindings?: number;
    highCount?: number;
    warningCount?: number;
    infoCount?: number;
    gpuMatches?: number;
    indexedFiles?: number;
    durationMs?: number;
  } | null;
};

// ============================================================
// App scaffold
// ============================================================

const appRoot = document.querySelector<HTMLDivElement>('#app');
if (!appRoot) throw new Error('App root was not found.');

appRoot.innerHTML = `
  <div class="app">
    <header class="topbar">
      <div class="brand">
        <div class="brand-mark" aria-hidden="true"></div>
        <span class="brand-name"><b>LegacyLens</b></span>
        <span class="brand-version">v0.4.0 · alpha</span>
      </div>
      <div class="topbar-status">
        <div class="status-chip off" id="chipGpu">
          <span class="dot"></span>
          <span>gpu-search</span>
          <span class="label-mono" id="chipGpuLabel">checking…</span>
        </div>
        <div class="status-chip off" id="chipOllama">
          <span class="dot"></span>
          <span>ollama</span>
          <span class="label-mono" id="chipOllamaLabel">checking…</span>
        </div>
      </div>
    </header>
    <nav class="tabs">
      <button class="tab active" data-tab="audit">Legacy Audit</button>
      <button class="tab" data-tab="review">Diff Review</button>
      <button class="tab" data-tab="analyze">.NET Analysis</button>
      <div class="tab-spacer"></div>
      <div class="kbd-hint" style="padding-right:8px">
        <span>switch</span><span class="kbd">⌃</span><span class="kbd">⇥</span>
      </div>
    </nav>
    <div class="workspace">
      <main class="main">
        <section class="run-card">
          <div class="run-card-head">
            <h2 id="runTitle">LEGACY AUDIT</h2>
            <span class="breadcrumb" id="runBreadcrumb">workflow / audit / new run</span>
          </div>
          <div class="run-row">
            <div class="input-group" style="flex:1">
              <span class="prefix">repo</span>
              <input id="repoPath" spellcheck="false" placeholder="D:\\Repos\\ExampleProject" />
              <button class="clear" id="clearRepoPath" title="Clear">×</button>
            </div>
            <button class="btn ghost" id="previewButton">Preview diff</button>
            <button class="btn primary" id="runButton">Run audit <span class="kbd">↵</span></button>
          </div>
          <div class="run-options">
            <span class="group-label">Enrichment</span>
            <label class="check"><input id="auditIncludeRoslyn" type="checkbox" checked /> Roslyn</label>
            <label class="check"><input id="auditIncludeGpuSearch" type="checkbox" checked /> gpu-search</label>
            <label class="check"><input id="auditIncludeDi" type="checkbox" checked /> Dependency injection</label>
            <label class="check"><input id="auditIncludePresets" type="checkbox" checked /> .NET presets</label>
            <label class="check"><input id="useLlm" type="checkbox" /> LLM summary</label>
            <div class="spacer"></div>
          </div>
        </section>
        <div id="reportArea" class="empty-state">
          <p>Run an audit, review, or analysis to see results.</p>
        </div>
      </main>
      <aside class="rail">
        <div class="rail-head">
          <h2>Saved reports</h2>
          <button class="btn ghost sm" id="refreshReportsButton">Refresh</button>
        </div>
        <div class="rail-filter">
          <button class="active" data-rail-filter="all">All</button>
          <button data-rail-filter="audit">Audits</button>
          <button data-rail-filter="review">Reviews</button>
        </div>
        <div id="railItems"><p style="padding:12px;color:var(--muted);font-size:var(--t-sm)">Loading…</p></div>
        <div class="rail-foot">local sqlite · data/legacylens.db</div>
      </aside>
    </div>
    <div id="toast" class="toast" aria-live="polite"></div>
  </div>
`;

// ============================================================
// State & element references
// ============================================================

let activeTab = 'audit';
let railFilter = 'all';
let activeReportId: string | null = null;
let allReports: ReportSummary[] = [];
let currentOllamaStatus: OllamaModelStatus | null = null;
const copyPayloads = new Map<string, string>();

const repoPathInput = getInput('repoPath');
const useLlmInput = getInput('useLlm');
const toast = getElement('toast');

// ============================================================
// Event bindings
// ============================================================

document.querySelectorAll<HTMLButtonElement>('.tab[data-tab]').forEach((btn) => {
  btn.addEventListener('click', () => switchTab(btn.dataset.tab ?? 'audit'));
});

document.querySelectorAll<HTMLButtonElement>('[data-rail-filter]').forEach((btn) => {
  btn.addEventListener('click', () => setRailFilter(btn.dataset.railFilter ?? 'all'));
});

bind('clearRepoPath', () => { repoPathInput.value = ''; repoPathInput.focus(); });
bind('previewButton', previewDiff);
bind('runButton', runPrimary);
bind('refreshReportsButton', loadReports);

document.getElementById('chipGpu')?.addEventListener('click', () => void checkStatus());
document.getElementById('chipOllama')?.addEventListener('click', () => void refreshOllamaStatus());

repoPathInput.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') void runPrimary();
});

// ============================================================
// Startup
// ============================================================

void checkStatus();
void refreshOllamaStatus();
void loadReports();

// ============================================================
// Tab / rail logic
// ============================================================

function switchTab(tab: string): void {
  activeTab = tab;
  document.querySelectorAll<HTMLButtonElement>('.tab[data-tab]').forEach((btn) => {
    btn.classList.toggle('active', btn.dataset.tab === tab);
  });
  const titleMap: Record<string, string> = { audit: 'LEGACY AUDIT', review: 'DIFF REVIEW', analyze: '.NET ANALYSIS' };
  const labelMap: Record<string, string> = { audit: 'Run audit', review: 'Run review', analyze: 'Run analysis' };
  const runTitle = document.getElementById('runTitle');
  const runBreadcrumb = document.getElementById('runBreadcrumb');
  const runButton = document.getElementById('runButton') as HTMLButtonElement | null;
  if (runTitle) runTitle.textContent = titleMap[tab] ?? tab.toUpperCase();
  if (runBreadcrumb) runBreadcrumb.textContent = `workflow / ${tab} / new run`;
  if (runButton && !runButton.disabled) {
    runButton.innerHTML = `${labelMap[tab] ?? 'Run'} <span class="kbd">↵</span>`;
  }
}

function setRailFilter(filter: string): void {
  railFilter = filter;
  document.querySelectorAll<HTMLButtonElement>('[data-rail-filter]').forEach((btn) => {
    btn.classList.toggle('active', btn.dataset.railFilter === filter);
  });
  renderRailItems();
}

function renderRailItems(): void {
  const railItems = document.getElementById('railItems');
  if (!railItems) return;

  const filtered = allReports.filter((r) => {
    if (railFilter === 'audit') return r.reportType === 'LegacyAudit';
    if (railFilter === 'review') return r.reportType !== 'LegacyAudit';
    return true;
  });

  if (filtered.length === 0) {
    railItems.innerHTML = '<p style="padding:12px;color:var(--muted);font-size:var(--t-sm)">No reports match this filter.</p>';
    return;
  }

  railItems.innerHTML = filtered.map(reportItem).join('');

  railItems.querySelectorAll<HTMLElement>('.report-item[data-view]').forEach((el) => {
    el.addEventListener('click', () => {
      activeReportId = el.dataset.view ?? null;
      document.querySelectorAll<HTMLElement>('.report-item').forEach((item) => {
        item.classList.toggle('active', item.dataset.view === activeReportId);
      });
      void viewReport(el.dataset.view ?? '', el.dataset.viewType);
    });
  });

  railItems.querySelectorAll<HTMLButtonElement>('[data-delete]').forEach((btn) => {
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      void deleteReport(btn.dataset.delete ?? '');
    });
  });

  railItems.querySelectorAll<HTMLButtonElement>('[data-export]').forEach((btn) => {
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      void exportAuditReport(btn.dataset.export ?? '', btn.dataset.format ?? '');
    });
  });
}

// ============================================================
// Status / Ollama
// ============================================================

async function checkStatus(): Promise<void> {
  const chip = document.getElementById('chipGpu');
  const label = document.getElementById('chipGpuLabel');
  try {
    const status = await api<SearchStatus>('/api/search/status');
    if (!chip || !label) return;
    const isOk = status.isAvailable;
    chip.className = isOk ? 'status-chip' : 'status-chip off';
    const fileCount = status.stats?.indexedFileCount;
    label.textContent = isOk
      ? `${status.stats?.device ?? 'ready'}${fileCount != null ? ' · ' + fileCount.toLocaleString() + ' files' : ''}`
      : (status.error ?? 'unavailable');
  } catch {
    if (chip) chip.className = 'status-chip off';
    if (label) label.textContent = 'error';
  }
}

async function refreshOllamaStatus(): Promise<void> {
  try {
    currentOllamaStatus = await api<OllamaModelStatus>('/api/llm/ollama/status');
    renderOllamaChip(currentOllamaStatus);
  } catch {
    const chip = document.getElementById('chipOllama');
    const label = document.getElementById('chipOllamaLabel');
    if (chip) chip.className = 'status-chip off';
    if (label) label.textContent = 'error';
  }
}

function renderOllamaChip(status: OllamaModelStatus): void {
  const chip = document.getElementById('chipOllama');
  const label = document.getElementById('chipOllamaLabel');
  if (!chip || !label) return;
  const isOk = status.serverReachable && status.modelInstalled;
  const isWarn = status.serverReachable && !status.modelInstalled;
  chip.className = isOk ? 'status-chip' : isWarn ? 'status-chip warn' : 'status-chip off';
  const model = status.configuredModel ?? 'unknown';
  label.textContent = isOk ? model : isWarn ? `${model} · not installed` : 'offline';
}


// ============================================================
// Primary actions
// ============================================================

async function runPrimary(): Promise<void> {
  if (activeTab === 'review') await runReview();
  else if (activeTab === 'analyze') await runAnalysis();
  else await runAudit();
}

async function previewDiff(): Promise<void> {
  await run('Loading diff preview…', async () => {
    const preview = await api<DiffPreview>('/api/review/diff/preview', {
      method: 'POST',
      body: JSON.stringify({ repoPath: repoPathInput.value }),
    });
    const reportArea = getElement('reportArea');
    reportArea.className = 'report-shell';
    reportArea.innerHTML = `
      <div class="report-head">
        <div>
          <p class="report-eyebrow">Diff Preview</p>
          <h1 class="report-title">${escapeHtml(repoTail(repoPathInput.value))}</h1>
          <div class="report-subtitle">${escapeHtml(repoPathInput.value)} · ${preview.changedFileCount ?? 0} changed file(s)</div>
        </div>
      </div>
      ${renderAuditSection('Changed files', preview.changedFileCount ?? 0, `
        <ul>
          ${(preview.files ?? []).map((f) => `<li style="font-family:var(--font-mono);font-size:var(--t-sm)">${escapeHtml(f.status ?? 'M')} ${escapeHtml(f.path ?? f.filePath ?? '')} <span style="color:var(--muted)">(+${f.additions ?? 0}/-${f.deletions ?? 0})</span></li>`).join('')}
        </ul>
      `)}
      ${renderAuditSection('Diff', null, `<pre>${escapeHtml(preview.diff || 'No diff content.')}</pre>`)}
    `;
  });
}

async function runReview(): Promise<void> {
  await run('Generating review…', async () => {
    const report = await api<ReviewReport>('/api/review/diff', {
      method: 'POST',
      body: JSON.stringify({ repoPath: repoPathInput.value, useLlm: useLlmInput.checked }),
    });
    renderReport(report);
    await loadReports();
  });
}

async function runAnalysis(): Promise<void> {
  await run('Running .NET analysis…', async () => {
    const analysis = await api<DotNetAnalysis>('/api/dotnet/analyze', {
      method: 'POST',
      body: JSON.stringify({ repoPath: repoPathInput.value, limitPerPreset: 8 }),
    });
    const reportArea = getElement('reportArea');
    reportArea.className = 'report-shell';
    const findings = analysis.findings ?? [];
    const rows = findings.map((f) => `
      <tr>
        <td class="col-sev"><span class="badge ${sevClass(f.severity)}">${sevLabel(f.severity ?? '')}</span></td>
        <td class="col-code">${escapeHtml(f.presetId ?? '')}</td>
        <td class="col-title">
          <div>${escapeHtml(f.filePath ?? 'unknown file')}${f.line ? `:${f.line}` : ''}</div>
          ${f.snippet ? `<div class="desc">${escapeHtml(f.snippet)}</div>` : ''}
        </td>
        <td class="col-loc">${escapeHtml(f.rationale ?? '')}</td>
      </tr>
    `).join('');
    reportArea.innerHTML = `
      <div class="report-head">
        <div>
          <p class="report-eyebrow">.NET Analysis</p>
          <h1 class="report-title">${escapeHtml(repoTail(repoPathInput.value))}</h1>
          <div class="report-subtitle">${escapeHtml(repoPathInput.value)} · ${findings.length} finding(s)</div>
        </div>
      </div>
      ${renderAuditSection('Findings', findings.length, findings.length
        ? `<table class="finding-table"><thead><tr><th>Sev</th><th>Preset</th><th>Location</th><th>Note</th></tr></thead><tbody>${rows}</tbody></table>`
        : '<p class="note">No findings returned.</p>'
      )}
    `;
  });
}

async function runAudit(): Promise<void> {
  await run('Running legacy audit…', async () => {
    const auditIncludeRoslyn = document.getElementById('auditIncludeRoslyn') as HTMLInputElement | null;
    const auditIncludeGpuSearch = document.getElementById('auditIncludeGpuSearch') as HTMLInputElement | null;
    const auditIncludePresets = document.getElementById('auditIncludePresets') as HTMLInputElement | null;
    const auditIncludeDi = document.getElementById('auditIncludeDi') as HTMLInputElement | null;

    const report = await api<LegacyAuditReport>('/api/audit/legacy-dotnet', {
      method: 'POST',
      body: JSON.stringify({
        repoPath: repoPathInput.value,
        useLlm: useLlmInput.checked,
        includeRoslyn: auditIncludeRoslyn?.checked ?? true,
        includeGpuSearch: auditIncludeGpuSearch?.checked ?? true,
        includeDotNetPresets: auditIncludePresets?.checked ?? true,
        includeDependencyInjection: auditIncludeDi?.checked ?? true,
      }),
    });
    renderAuditReport(report);
    await loadReports();
  });
}

// ============================================================
// Report management
// ============================================================

async function loadReports(): Promise<void> {
  try {
    const items = await api<ReportSummary[]>('/api/reports');
    allReports = items;
    renderRailItems();
  } catch {
    const railItems = document.getElementById('railItems');
    if (railItems) railItems.innerHTML = '<p style="padding:12px;color:var(--sev-high);font-size:var(--t-sm)">Could not load reports.</p>';
  }
}

async function viewReport(id: string, reportType?: string): Promise<void> {
  await run('Loading report…', async () => {
    if (reportType === 'LegacyAudit') {
      const report = await api<LegacyAuditReport>(`/api/audit/reports/${encodeURIComponent(id)}`);
      renderAuditReport(report);
    } else {
      const report = await api<ReviewReport>(`/api/reports/${encodeURIComponent(id)}`);
      renderReport(report);
    }
  });
}

async function deleteReport(id: string): Promise<void> {
  if (!window.confirm('Delete this saved report?')) return;
  await run('Deleting report…', async () => {
    await api(`/api/reports/${encodeURIComponent(id)}`, { method: 'DELETE' });
    if (activeReportId === id) {
      activeReportId = null;
      const reportArea = getElement('reportArea');
      reportArea.className = 'empty-state';
      reportArea.innerHTML = '<p>Run an audit, review, or analysis to see results.</p>';
    }
    await loadReports();
    showToast('Report deleted.');
  });
}

async function exportAuditReport(id: string, format: string): Promise<void> {
  if (!id || (format !== 'markdown' && format !== 'html')) {
    showToast('Export is unavailable for this report.', true);
    return;
  }
  await run(`Exporting ${format}…`, async () => {
    const response = await fetch(`/api/audit/reports/${encodeURIComponent(id)}/export/${format}`);
    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || `${response.status} ${response.statusText}`);
    }
    const blob = await response.blob();
    const fileName = fileNameFromDisposition(response.headers.get('content-disposition'))
      ?? `legacy-audit-${id}.${format === 'markdown' ? 'md' : 'html'}`;
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
    showToast(`Exported ${fileName}.`);
  });
}

function fileNameFromDisposition(disposition: string | null): string | null {
  if (!disposition) return null;
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(disposition);
  return match ? decodeURIComponent(match[1].replaceAll('"', '').trim()) : null;
}

// ============================================================
// Audit report rendering
// ============================================================

function renderAuditReport(report: LegacyAuditReport): void {
  copyPayloads.clear();
  const markdown = report.markdown ?? '';
  registerCopy('audit-markdown', markdown);
  activeReportId = report.reportId ?? null;

  const reportArea = getElement('reportArea');
  reportArea.className = 'report-shell';
  reportArea.innerHTML = `
    ${renderReportHead(report)}
    ${renderMetricStrip(report)}
    ${renderAuditSummarySection(report)}
    ${renderTechnologySignals(report.technologySignals ?? [])}
    ${renderArchitectureSignals(report.architectureSignals ?? [])}
    ${renderAuditFindings(report.riskFindings ?? [])}
    ${renderAuditRoslynSummary(report.roslynSummary ?? null)}
    ${renderAuditDiSummary(report.dependencyInjectionSummary ?? null)}
    ${renderAuditGpuSearchSummary(report.gpuSearchSummary ?? null)}
    ${renderNextSteps(report.recommendedNextSteps ?? [])}
    ${report.llmSummary ? renderAuditSection('LLM Summary', null, `<div class="llm-summary">${escapeHtml(report.llmSummary)}</div>`) : ''}
    ${markdown ? renderRawMarkdown(markdown) : ''}
  `;

  reportArea.querySelectorAll<HTMLButtonElement>('[data-copy]').forEach((btn) => {
    btn.addEventListener('click', () => void copyText(btn.dataset.copy ?? ''));
  });
  reportArea.querySelectorAll<HTMLButtonElement>('[data-export]').forEach((btn) => {
    btn.addEventListener('click', () => void exportAuditReport(report.reportId ?? '', btn.dataset.format ?? ''));
  });

  document.querySelectorAll<HTMLElement>('.report-item').forEach((item) => {
    item.classList.toggle('active', item.dataset.view === activeReportId);
  });
}

function renderReportHead(report: LegacyAuditReport): string {
  const id = report.reportId ?? '';
  const date = report.generatedAtUtc ? shortDate(report.generatedAtUtc) : '—';
  return `
    <div class="report-head">
      <div>
        <p class="report-eyebrow">Legacy .NET Audit · Report</p>
        <h1 class="report-title">${escapeHtml(repoTail(report.repoPath))}</h1>
        <div class="report-subtitle">${escapeHtml(report.repoPath ?? '')} · id <code>${escapeHtml(id.slice(0, 8))}</code> · generated ${escapeHtml(date)}</div>
      </div>
      <div class="report-actions">
        <button class="btn ghost sm" data-copy="audit-markdown">Copy Markdown</button>
        <button class="btn ghost sm" data-export data-format="html">Export HTML</button>
        <button class="btn ghost sm" data-export data-format="markdown">Export MD</button>
      </div>
    </div>
  `;
}

function renderMetricStrip(report: LegacyAuditReport): string {
  const m = report.metrics;
  const techCount = m?.technologySignals ?? report.technologySignals?.length ?? 0;
  const riskCount = m?.riskFindings ?? report.riskFindings?.length ?? 0;
  const findings = report.riskFindings ?? [];
  const highCount = m?.highCount ?? findings.filter((f) => { const v = (f.severity ?? '').toLowerCase(); return v === 'high' || v === 'critical'; }).length;
  const warnCount = m?.warningCount ?? findings.filter((f) => { const v = (f.severity ?? '').toLowerCase(); return v === 'warning' || v === 'medium'; }).length;
  const infoCount = m?.infoCount ?? findings.filter((f) => (f.severity ?? '').toLowerCase() === 'info').length;
  const gpuMatches = m?.gpuMatches ?? report.gpuSearchSummary?.totalResults ?? 0;
  const gpuQueries = report.gpuSearchSummary?.queriesRun ?? 0;
  const durationMs = m?.durationMs;

  return `
    <div class="metric-strip">
      <div class="metric">
        <div class="label">Repository</div>
        <div class="value" style="font-size:14px;font-family:var(--font-mono);letter-spacing:0">${escapeHtml(repoTail(report.repoPath))}</div>
      </div>
      <div class="metric info">
        <div class="label">Tech signals</div>
        <div class="value">${techCount}<span class="sub">detected</span></div>
      </div>
      <div class="metric ${riskCount > 0 && highCount > 0 ? 'bad' : riskCount > 0 ? 'warn' : ''}">
        <div class="label">Risk findings</div>
        <div class="value">${riskCount}<span class="sub">${highCount}H · ${warnCount}W · ${infoCount}I</span></div>
      </div>
      <div class="metric">
        <div class="label">gpu-search matches</div>
        <div class="value">${gpuMatches}<span class="sub">${gpuQueries} queries</span></div>
      </div>
      <div class="metric">
        <div class="label">Duration</div>
        <div class="value">${durationMs != null ? (durationMs / 1000).toFixed(1) : '—'}<span class="sub">${durationMs != null ? 'seconds' : ''}</span></div>
      </div>
    </div>
  `;
}

function renderAuditSummarySection(report: LegacyAuditReport): string {
  const ws = report.workspaceSummary;
  const wsMeta = ws?.selectedWorkspaceKind ? `workspace · ${ws.selectedWorkspaceKind}` : undefined;
  const wsCallout = ws ? `
    <div class="callout" style="margin-top:14px">
      <div class="callout-icon">i</div>
      <div>
        <strong>Workspace resolved:</strong>
        <span style="font-family:var(--font-mono);font-size:var(--t-sm);margin-left:6px">${escapeHtml(ws.selectedWorkspacePath ?? 'n/a')}</span>
        <span style="color:var(--muted);margin-left:8px;font-size:var(--t-sm)">(${ws.totalCandidates ?? 0} candidates · ${ws.slnCount ?? 0} .sln · ${ws.csprojCount ?? 0} .csproj)</span>
      </div>
    </div>
  ` : '';
  return renderAuditSection('Summary', null, `
    <p class="prose" style="margin-top:0">${escapeHtml(report.summary ?? '')}</p>
    ${wsCallout}
  `, wsMeta);
}

function renderTechnologySignals(signals: TechnologySignal[]): string {
  if (signals.length === 0) {
    return renderAuditSection('Technology signals', 0, '<p class="note">No signals detected.</p>');
  }
  const items = signals.map((s) => `
    <article class="signal">
      <div class="signal-icon">${escapeHtml(categoryGlyph(s.category ?? ''))}</div>
      <div class="signal-body">
        <div class="signal-meta">
          <span class="badge category">${escapeHtml(s.category ?? 'Signal')}</span>
          <span class="badge confidence">conf · ${escapeHtml(s.confidence ?? 'unknown')}</span>
        </div>
        <p class="signal-title">${escapeHtml(s.name ?? 'Signal')}</p>
        ${s.filePath ? `<p class="signal-path">${escapeHtml(s.filePath)}</p>` : ''}
        <p class="signal-evidence">${escapeHtml(s.evidence ?? '')}</p>
      </div>
    </article>
  `).join('');
  return renderAuditSection('Technology signals', signals.length, `<div class="signal-grid">${items}</div>`);
}

function renderArchitectureSignals(signals: ArchitectureSignal[]): string {
  if (signals.length === 0) return '';
  const items = signals.map((s) => `
    <div style="display:grid;grid-template-columns:auto 1fr;gap:16px;align-items:start;padding:4px 0">
      <span class="badge confidence">conf · ${escapeHtml(s.confidence ?? 'unknown')}</span>
      <div>
        <p style="margin:0 0 4px;font-size:var(--t-base);font-weight:500">${escapeHtml(s.name ?? '')}</p>
        <p class="prose" style="margin:0 0 4px">${escapeHtml(s.message ?? '')}</p>
        <p style="margin:0;font-family:var(--font-mono);font-size:var(--t-xs);color:var(--muted)">${escapeHtml(s.evidence ?? '')}</p>
      </div>
    </div>
  `).join('');
  return renderAuditSection('Architecture signals', signals.length, items);
}

function renderAuditFindings(findings: AuditFinding[]): string {
  if (findings.length === 0) {
    return renderAuditSection('Risk findings', 0, '<p class="note">No risk findings.</p>');
  }
  const highCount = findings.filter((f) => { const v = (f.severity ?? '').toLowerCase(); return v === 'high' || v === 'critical'; }).length;
  const warnCount = findings.filter((f) => { const v = (f.severity ?? '').toLowerCase(); return v === 'warning' || v === 'medium'; }).length;
  const infoCount = findings.filter((f) => (f.severity ?? '').toLowerCase() === 'info').length;
  const meta = `${highCount} high · ${warnCount} warning · ${infoCount} info`;
  const rows = findings.map((f) => `
    <tr>
      <td class="col-sev"><span class="badge ${sevClass(f.severity)}">${sevLabel(f.severity ?? '')}</span></td>
      <td class="col-code">${escapeHtml(f.code ?? '')}</td>
      <td class="col-title">
        <div>${escapeHtml(f.title ?? f.code ?? 'Finding')}</div>
        <div class="desc">${escapeHtml(f.message ?? '')}</div>
      </td>
      <td class="col-loc">${f.filePath ? escapeHtml(f.filePath) + (f.line ? `:${f.line}` : '') : '—'}</td>
    </tr>
  `).join('');
  return renderAuditSection('Risk findings', findings.length,
    `<table class="finding-table"><thead><tr><th>Severity</th><th>Rule</th><th>Finding</th><th>Location</th></tr></thead><tbody>${rows}</tbody></table>`,
    meta
  );
}

function renderAuditRoslynSummary(roslyn: AuditRoslynSummary | null): string {
  if (!roslyn) return renderAuditSection('Roslyn summary', null, '<p class="note">Roslyn analysis was not requested.</p>');

  const meta = roslyn.workspaceLoaded ? 'compiler-aware' : 'workspace load failed';

  if (!roslyn.workspaceLoaded) {
    return renderAuditSection('Roslyn summary', null, `
      <div class="callout warn">
        <div class="callout-icon">!</div>
        <div>
          <strong>Roslyn workspace could not be loaded.</strong>
          <div style="margin-top:4px;color:var(--muted);font-family:var(--font-mono);font-size:var(--t-xs)">Workspace · ${escapeHtml(roslyn.workspacePath ?? 'n/a')}</div>
          ${roslyn.errorMessage ? `<div style="margin-top:6px;color:var(--ink-2)">${escapeHtml(roslyn.errorMessage)}</div>` : ''}
        </div>
      </div>
      ${roslyn.warnings?.length ? detailsList('Warnings', roslyn.warnings) : ''}
    `, meta);
  }

  return renderAuditSection('Roslyn summary', null, `
    <div class="kv-grid">
      ${kv('Workspace kind', roslyn.workspaceKind ?? 'n/a')}
      ${kv('Projects', String(roslyn.projectCount ?? 0))}
      ${kv('Documents', String(roslyn.documentCount ?? 0))}
      ${kv('Symbols', String(roslyn.symbolCount ?? 0))}
      ${kv('Classes', String(roslyn.classCount ?? 0))}
      ${kv('Interfaces', String(roslyn.interfaceCount ?? 0))}
      ${kv('Methods', String(roslyn.methodCount ?? 0))}
      ${kv('Path', fileTail(roslyn.workspacePath ?? '', 36), true)}
    </div>
    ${roslyn.warnings?.length ? detailsList('Warnings', roslyn.warnings) : ''}
  `, meta);
}

function renderAuditDiSummary(di: DiSummary | null): string {
  if (!di) return renderAuditSection('Dependency injection summary', null, '<p class="note">DI analysis was not requested.</p>');

  const lifetimes = di.registrationsByLifetime ?? {};
  const lifetimeStr = `S ${lifetimes['Singleton'] ?? 0} · Sc ${lifetimes['Scoped'] ?? 0} · T ${lifetimes['Transient'] ?? 0}`;

  const diFindings = (di.findings ?? []).map((f) => `
    <tr>
      <td class="col-sev"><span class="badge ${sevClass(f.severity)}">${sevLabel(f.severity ?? '')}</span></td>
      <td class="col-code">${escapeHtml(f.code ?? '')}</td>
      <td class="col-title"><div>${escapeHtml(f.message ?? '')}</div></td>
      <td class="col-loc">${f.filePath ? escapeHtml(f.filePath) + (f.line ? `:${f.line}` : '') : '—'}</td>
    </tr>
  `).join('');

  return renderAuditSection('Dependency injection summary', null, `
    <div class="kv-grid">
      ${kv('Registrations', String(di.registrationCount ?? 0))}
      ${kv('Constructor deps', String(di.constructorDependencyCount ?? 0))}
      ${kv('Findings', String(di.findingCount ?? 0))}
      ${kv('By lifetime', lifetimeStr, true)}
    </div>
    ${di.findingCount === 0 ? `<p class="note" style="margin-top:10px">No IServiceCollection registrations detected. Expected for .NET Framework projects without modern DI.</p>` : ''}
    ${diFindings ? `<table class="finding-table" style="margin-top:12px"><thead><tr><th>Sev</th><th>Rule</th><th>Finding</th><th>Location</th></tr></thead><tbody>${diFindings}</tbody></table>` : ''}
  `, 'static · advisory');
}

function renderAuditGpuSearchSummary(gpuSearch: GpuSearchAuditSummary | null): string {
  if (!gpuSearch) return renderAuditSection('gpu-search signal scan', null, '<p class="note">gpu-search was not requested.</p>');

  if (!gpuSearch.wasAvailable) {
    return renderAuditSection('gpu-search signal scan', null, `
      <div class="callout warn">
        <div class="callout-icon">!</div>
        <div>
          <strong>gpu-search was unavailable.</strong>
          ${gpuSearch.errorMessage ? `<div style="margin-top:6px;color:var(--ink-2)">${escapeHtml(gpuSearch.errorMessage)}</div>` : ''}
        </div>
      </div>
    `);
  }

  const modeMeta = `mode · ${gpuSearch.usedSignalScan ? 'scan/signals' : 'fallback'} · ${gpuSearch.queriesRun ?? 0} queries`;
  const cats = (gpuSearch.signalCategories ?? []).map((c) => `<span class="cat-pill">${escapeHtml(c)}</span>`).join('');
  const rows = (gpuSearch.results ?? []).map((r) => `
    <div class="hit-row">
      <div class="hit-query">${escapeHtml(r.query ?? '')}</div>
      <div class="hit-detail">
        <div class="hit-loc">${escapeHtml(r.filePath ?? '')}${r.line != null ? `<span class="line">:${r.line}</span>` : ''}</div>
        <div class="hit-snippet">${escapeHtml(r.snippet ?? '')}</div>
      </div>
    </div>
  `).join('');

  return renderAuditSection('gpu-search signal scan', gpuSearch.totalResults ?? 0, `
    <div class="hits">
      ${cats ? `<div class="hits-cats">${cats}</div>` : ''}
      ${rows}
    </div>
    <p class="note" style="margin-top:10px">Results are heuristic / retrieval-based and not compiler-verified. Showing top ${gpuSearch.results?.length ?? 0} of ${gpuSearch.totalResults ?? 0}.</p>
  `, modeMeta);
}

function renderNextSteps(steps: string[]): string {
  if (steps.length === 0) return renderAuditSection('Recommended next steps', 0, '<p class="note">No recommendations generated.</p>');
  return renderAuditSection('Recommended next steps', steps.length,
    `<ol class="steps">${steps.map((s) => `<li>${escapeHtml(s)}</li>`).join('')}</ol>`
  );
}

// ============================================================
// Diff review rendering
// ============================================================

function renderReport(report: ReviewReport): void {
  copyPayloads.clear();
  const markdown = report.markdown ?? '';
  const context = report.gpuSearchContext ?? null;
  const roslynCtx = report.roslynContext ?? null;
  const llmSummary = report.llmSummary ?? '';

  registerCopy('markdown', markdown);
  if (context) registerCopy('context', JSON.stringify(context, null, 2));
  if (llmSummary) registerCopy('llm', llmSummary);

  activeReportId = report.reportId ?? report.id ?? null;

  const reportArea = getElement('reportArea');
  reportArea.className = 'report-shell';
  const id = report.reportId ?? report.id ?? 'report';
  const generated = report.generatedAtUtc ?? report.createdAtUtc ?? report.createdAt;

  reportArea.innerHTML = `
    <div class="report-head">
      <div>
        <p class="report-eyebrow">Diff Review · Report</p>
        <h1 class="report-title">${escapeHtml(repoTail(report.repoPath))}</h1>
        <div class="report-subtitle">${escapeHtml(report.repoPath ?? '')} · id <code>${escapeHtml(id.slice(0, 8))}</code> · ${generated ? shortDate(generated) : '—'}</div>
      </div>
      <div class="report-actions">
        <button class="btn ghost sm" data-copy="markdown">Copy Markdown</button>
        ${context ? '<button class="btn ghost sm" data-copy="context">Copy gpu-search context</button>' : ''}
        ${llmSummary ? '<button class="btn ghost sm" data-copy="llm">Copy LLM summary</button>' : ''}
      </div>
    </div>
    ${renderReviewSummary(report)}
    ${renderFindings(report.findings ?? [])}
    ${renderGpuSearchContext(context)}
    ${renderRoslynContext(roslynCtx)}
    ${llmSummary ? renderAuditSection('LLM Summary', null, `<div class="llm-summary">${escapeHtml(llmSummary)}</div>`) : ''}
    ${markdown ? renderRawMarkdown(markdown) : ''}
  `;

  reportArea.querySelectorAll<HTMLButtonElement>('[data-copy]').forEach((btn) => {
    btn.addEventListener('click', () => void copyText(btn.dataset.copy ?? ''));
  });

  document.querySelectorAll<HTMLElement>('.report-item').forEach((item) => {
    item.classList.toggle('active', item.dataset.view === activeReportId);
  });
}

function renderReviewSummary(report: ReviewReport): string {
  const generated = report.generatedAtUtc ?? report.createdAtUtc ?? report.createdAt;
  return renderAuditSection('Summary', null, `
    <div class="kv-grid">
      ${kv('Repository', report.repoPath ?? 'n/a')}
      ${kv('Created', generated ? formatDate(generated) : 'n/a')}
      ${kv('Changed files', String(report.changedFileCount ?? report.files?.length ?? 'n/a'))}
      ${kv('Provider / mode', report.providerName ?? report.llmProvider ?? report.reviewMode ?? report.analysisMode ?? 'n/a')}
    </div>
  `);
}

function renderFindings(findings: ReviewFinding[]): string {
  if (findings.length === 0) {
    return renderAuditSection('Findings', 0, '<p class="note">No findings returned.</p>');
  }
  const highCount = findings.filter((f) => { const v = (f.severity ?? '').toLowerCase(); return v === 'high' || v === 'critical'; }).length;
  const warnCount = findings.filter((f) => { const v = (f.severity ?? '').toLowerCase(); return v === 'warning' || v === 'medium'; }).length;
  const infoCount = findings.filter((f) => (f.severity ?? '').toLowerCase() === 'info').length;
  const meta = `${highCount} high · ${warnCount} warning · ${infoCount} info`;
  const rows = findings.map((f) => {
    const loc = f.path ?? f.filePath;
    const line = formatLineRange(f);
    return `
      <tr>
        <td class="col-sev"><span class="badge ${sevClass(f.severity)}">${sevLabel(f.severity ?? '')}</span></td>
        <td class="col-code">${escapeHtml(f.ruleId ?? '')}${f.category ? `<div style="margin-top:2px;color:var(--subtle)">${escapeHtml(f.category)}</div>` : ''}</td>
        <td class="col-title">
          <div>${escapeHtml(f.title ?? f.message ?? 'Finding')}</div>
          ${f.description ? `<div class="desc">${escapeHtml(f.description)}</div>` : ''}
        </td>
        <td class="col-loc">${loc ? escapeHtml(loc) + (line ? ':' + escapeHtml(line) : '') : '—'}</td>
      </tr>
    `;
  }).join('');
  return renderAuditSection('Findings', findings.length,
    `<table class="finding-table"><thead><tr><th>Severity</th><th>Rule</th><th>Finding</th><th>Location</th></tr></thead><tbody>${rows}</tbody></table>`,
    meta
  );
}

function renderGpuSearchContext(context: GpuSearchContext | null): string {
  if (!context) return renderAuditSection('gpu-search Context', null, '<p class="note">No gpu-search context was included in this report.</p>');

  if (context.wasAvailable === false) {
    return renderAuditSection('gpu-search Context', null,
      `<div class="callout warn"><div class="callout-icon">!</div><div><strong>gpu-search unavailable:</strong> ${escapeHtml(context.unavailableReason ?? 'No reason provided.')}</div></div>`
    );
  }

  const warnings = context.warnings?.length ? detailsList('Global warnings', context.warnings) : '';
  const limitations = context.limitations?.length ? detailsList('Global limitations', context.limitations) : '';
  const files = context.files?.length
    ? context.files.map(renderChangedFileContext).join('')
    : '<p class="note">No per-file gpu-search context returned.</p>';

  return renderAuditSection('gpu-search Context', context.files?.length ?? null, `${warnings}${limitations}<div class="context-list">${files}</div>`);
}

function renderChangedFileContext(file: ChangedFileContext): string {
  const impact = file.dependencyImpact;
  const impactedFiles = normalizeImpactedFiles(impact?.impactedFiles ?? file.impactedFiles, impact?.directImporters);
  const warnings = impact?.warnings ?? file.warnings ?? [];
  const limitations = impact?.limitations ?? file.limitations ?? [];
  const skeleton = file.skeleton;
  const skeletonContent = typeof skeleton === 'string' ? skeleton : skeleton?.content;

  return `
    <article class="context-card">
      <div style="display:flex;align-items:flex-start;justify-content:space-between;gap:8px;margin-bottom:6px">
        <h4 style="margin:0;font-family:var(--font-mono);font-size:var(--t-sm);color:var(--ink);word-break:break-all">${escapeHtml(file.filePath ?? file.path ?? 'Unknown file')}</h4>
        <div style="display:flex;gap:4px;flex-shrink:0">
          ${impact?.confidence ?? file.confidence ? `<span class="badge confidence">${escapeHtml(impact?.confidence ?? file.confidence ?? '')}</span>` : ''}
          ${impact?.analysisMode ?? file.analysisMode ? `<span class="badge">${escapeHtml(impact?.analysisMode ?? file.analysisMode ?? '')}</span>` : ''}
        </div>
      </div>
      ${file.error ? `<p class="error" style="font-size:var(--t-sm)">${escapeHtml(file.error)}</p>` : ''}
      <p style="font-size:var(--t-sm);color:var(--muted);margin:0 0 8px">${escapeHtml(impact?.summary ?? dependencySummary(impact, impactedFiles))}</p>
      ${impactedFiles.length ? impactedFilesBlock(impactedFiles) : ''}
      ${warnings.length ? detailsList('Warnings', warnings) : ''}
      ${limitations.length ? detailsList('Limitations', limitations) : ''}
      ${renderRelatedResults(file.relatedResults ?? [])}
      ${skeletonContent
        ? `<details><summary>Skeleton preview${typeof skeleton === 'object' && skeleton?.language ? ` · ${escapeHtml(skeleton.language)}` : ''}</summary><pre>${escapeHtml(skeletonContent)}</pre></details>`
        : ''}
    </article>
  `;
}

function renderRelatedResults(results: RelatedCodeResult[]): string {
  if (results.length === 0) return '';
  return `
    <div class="related-list" style="margin-top:8px">
      <h4>Related search results</h4>
      ${results.map((result) => `
        <article class="related-card">
          <div style="display:flex;align-items:center;justify-content:space-between;gap:8px;margin-bottom:4px">
            <strong style="font-family:var(--font-mono);font-size:var(--t-sm)">${escapeHtml(result.file ?? result.filePath ?? 'Unknown')}${formatLineRange(result) ? `:${escapeHtml(formatLineRange(result))}` : ''}</strong>
            <span class="badge">${escapeHtml(result.engine ?? 'search')}${result.score != null ? ` · ${result.score.toFixed(2)}` : ''}</span>
          </div>
          ${result.reason ? `<p style="font-size:var(--t-xs);color:var(--muted);margin:0 0 4px">${escapeHtml(result.reason)}</p>` : ''}
          ${result.snippet ? `<pre>${escapeHtml(result.snippet)}</pre>` : ''}
        </article>
      `).join('')}
    </div>
  `;
}

function renderRoslynContext(context: RoslynReviewContext | null): string {
  if (!context) return '';

  if (!context.success || (!context.changedSymbols?.length && context.errorMessage)) {
    return renderAuditSection('Roslyn Reference Context', null, `
      <div class="callout warn">
        <div class="callout-icon">!</div>
        <div>
          <strong>Roslyn reference analysis was unavailable.</strong>
          ${context.errorMessage ? `<div style="margin-top:6px;color:var(--ink-2)">${escapeHtml(context.errorMessage)}</div>` : ''}
        </div>
      </div>
    `);
  }

  const symbols = context.changedSymbols ?? [];
  if (symbols.length === 0) return renderAuditSection('Roslyn Reference Context', 0, '<p class="note">No changed C# symbols were matched.</p>');

  const allRefs = context.symbolReferences ?? [];
  const symbolCards = symbols.map((symbol) => {
    const refs = allRefs.filter((r) => r.symbolName === symbol.name && r.symbolFullName === symbol.fullName && !r.isDefinition);
    const refRows = refs.slice(0, 10).map((r) => {
      const container = r.containingSymbol ? ` <span style="color:var(--muted)">— in ${escapeHtml(r.containingSymbol)}</span>` : '';
      return `<li style="font-family:var(--font-mono);font-size:var(--t-xs)">${escapeHtml(r.filePath ?? '')}:${r.line ?? '?'}${container}</li>`;
    }).join('');

    return `
      <article class="context-card">
        <div style="display:flex;align-items:center;gap:8px;margin-bottom:8px">
          <span style="font-family:var(--font-mono);font-size:var(--t-sm);font-weight:600">${escapeHtml(symbol.name ?? 'Unknown')}</span>
          <span class="badge">${escapeHtml(symbol.kind ?? 'symbol')}</span>
        </div>
        <div class="kv-grid">
          ${kv('Project', symbol.projectName ?? 'n/a')}
          ${kv('Definition', symbol.filePath ? `${symbol.filePath}:${symbol.line ?? '?'}` : 'n/a')}
          ${kv('References', String(refs.length))}
        </div>
        ${refs.length ? `<div class="mini-block"><h4>References</h4><ul>${refRows}</ul></div>` : ''}
      </article>
    `;
  }).join('');

  const warnings = context.warnings?.length ? detailsList('Warnings', context.warnings) : '';
  const workspaceInfo = context.workspacePath
    ? `<p style="font-family:var(--font-mono);font-size:var(--t-xs);color:var(--muted);margin:0 0 12px">${escapeHtml(context.workspacePath)}${context.workspaceKind ? ` (${escapeHtml(context.workspaceKind)})` : ''}</p>`
    : '';

  return renderAuditSection('Roslyn Reference Context', symbols.length, `${workspaceInfo}${warnings}<div class="context-list">${symbolCards}</div>`);
}

function renderRawMarkdown(markdown: string): string {
  return `
    <div class="section">
      <details>
        <summary>Raw Markdown</summary>
        <pre style="margin-top:8px">${escapeHtml(markdown || 'No Markdown returned.')}</pre>
      </details>
    </div>
  `;
}

// ============================================================
// Rail item rendering
// ============================================================

function reportItem(report: ReportSummary): string {
  const id = report.reportId ?? report.id ?? '';
  const type = report.reportType ?? 'DiffReview';
  const isAudit = type === 'LegacyAudit';
  const typeClass = isAudit ? 'audit' : 'review';
  const typeLabel = isAudit ? 'audit' : 'review';
  const repoLabel = (() => {
    const p = report.repoPath ?? '';
    const parts = p.replace(/\\/g, '/').split('/').filter(Boolean);
    return parts.length >= 2 ? parts.slice(-2).join('\\') : p;
  })();
  const ts = report.generatedAtUtc ?? report.createdAtUtc ?? '';
  const findingCount = report.findingCount ?? (report as ReportSummary & { findings?: number }).findings;
  const fileCount = report.changedFileCount;
  const provider = report.llmProvider ?? report.providerName ?? '—';
  const isActive = id === activeReportId;

  return `
    <div class="report-item ${isActive ? 'active' : ''}" data-view="${escapeHtml(id)}" data-view-type="${escapeHtml(type)}">
      <div class="item-top">
        <span class="item-type ${typeClass}">${typeLabel}</span>
        <span class="item-time">${ts ? relTime(ts) : '—'}</span>
      </div>
      <div class="item-repo">${escapeHtml(repoLabel)}</div>
      <div class="item-bot">
        ${findingCount != null ? `<span>${findingCount} findings</span><span class="dot-sep">·</span>` : ''}
        ${fileCount != null ? `<span>${fileCount.toLocaleString()} files</span><span class="dot-sep">·</span>` : ''}
        <span>llm · ${escapeHtml(provider)}</span>
        <span style="margin-left:auto;display:flex;gap:4px">
          ${isAudit ? `<button class="btn ghost sm" data-export="${escapeHtml(id)}" data-format="html" style="padding:0 6px;height:20px;font-size:10px">HTML</button>` : ''}
          <button class="btn danger sm" data-delete="${escapeHtml(id)}" style="padding:0 6px;height:20px;font-size:10px">Del</button>
        </span>
      </div>
    </div>
  `;
}

// ============================================================
// Utilities
// ============================================================

function renderAuditSection(title: string, count: number | null | undefined, content: string, sectionMeta?: string): string {
  const countHtml = count != null ? `<span style="color:var(--subtle);margin-left:8px;font-weight:500">· ${count}</span>` : '';
  const metaHtml = sectionMeta ? `<span class="head-meta">${escapeHtml(sectionMeta)}</span>` : '';
  return `
    <section class="section">
      <div class="section-head">
        <h3>${escapeHtml(title)}${countHtml}</h3>
        ${metaHtml}
      </div>
      ${content}
    </section>
  `;
}

function kv(label: string, value: string, mono = false): string {
  return `<div class="kv"><div class="label">${escapeHtml(label)}</div><div class="value${mono ? ' mono' : ''}">${escapeHtml(value)}</div></div>`;
}

function relTime(iso: string): string {
  const ms = Date.now() - new Date(iso).getTime();
  const m = Math.floor(ms / 60000);
  if (m < 1) return 'just now';
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
}

function shortDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString('en-US', { month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false });
}

function repoTail(path?: string): string {
  if (!path) return 'Unknown repository';
  const parts = path.replace(/\\/g, '/').split('/').filter(Boolean);
  return parts[parts.length - 1] ?? path;
}

function fileTail(p: string, max = 60): string {
  if (!p) return '';
  if (p.length <= max) return p;
  return '…' + p.slice(p.length - max);
}

function categoryGlyph(cat: string): string {
  const m: Record<string, string> = {
    Framework: 'FW', Dependencies: 'PK', Configuration: 'CF',
    Quality: 'QA', Architecture: 'AR', Security: 'SE', Database: 'DB',
  };
  return m[cat] ?? (cat ? cat.slice(0, 2).toUpperCase() : '··');
}

function sevClass(s?: string): string {
  const v = (s ?? '').toLowerCase();
  if (v === 'high' || v === 'critical') return 'sev-high';
  if (v === 'warning' || v === 'medium') return 'sev-warning';
  if (v === 'info' || v === 'low') return 'sev-info';
  if (v === 'ok' || v === 'success') return 'sev-ok';
  return 'sev-info';
}

function sevLabel(s: string): string {
  if (!s) return '—';
  return s.charAt(0).toUpperCase() + s.slice(1).toLowerCase();
}

async function run(label: string, action: () => Promise<void>): Promise<void> {
  const runButton = document.getElementById('runButton') as HTMLButtonElement | null;
  const savedHtml = runButton?.innerHTML ?? '';
  if (runButton) { runButton.disabled = true; runButton.textContent = label; }
  try {
    await action();
  } catch (error) {
    showToast(errorMessage(error), true);
  } finally {
    if (runButton) { runButton.disabled = false; runButton.innerHTML = savedHtml; }
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
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

function detailsList(title: string, rows: string[]): string {
  return `
    <details>
      <summary>${escapeHtml(title)} (${rows.length})</summary>
      <ul>${rows.map((row) => `<li>${escapeHtml(row)}</li>`).join('')}</ul>
    </details>
  `;
}

function impactedFilesBlock(files: ImpactedFile[]): string {
  return `
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${files.map((file) => {
          const path = file.file ?? file.filePath ?? file.path ?? 'Unknown file';
          const hops = file.hops != null ? ` · ${file.hops} hop${file.hops === 1 ? '' : 's'}` : '';
          const reason = file.reason ? ` <span style="color:var(--muted)">— ${escapeHtml(file.reason)} <em>(heuristic)</em></span>` : '';
          return `<li style="font-family:var(--font-mono);font-size:var(--t-xs)">${escapeHtml(path)}${hops}${reason}</li>`;
        }).join('')}
      </ul>
    </div>
  `;
}

function normalizeImpactedFiles(
  impactedFiles?: ImpactedFile[] | string[] | null,
  directImporters?: string[] | null,
): ImpactedFile[] {
  if (Array.isArray(impactedFiles) && impactedFiles.length > 0) {
    return impactedFiles.map((file) => (typeof file === 'string' ? { file, hops: 1 } : file));
  }
  return (directImporters ?? []).map((file) => ({ file, hops: 1 }));
}

function dependencySummary(impact: DependencyImpactSummary | null | undefined, impactedFiles: ImpactedFile[]): string {
  if (!impact) return 'No dependency impact summary returned.';
  const total = impact.totalImpacted ?? impactedFiles.length;
  return `${total} impacted file(s) identified.`;
}

function formatLineRange(value: { line?: number | null; lineStart?: number | null; lineEnd?: number | null }): string {
  if (value.lineStart && value.lineEnd) {
    return value.lineStart === value.lineEnd ? String(value.lineStart) : `${value.lineStart}-${value.lineEnd}`;
  }
  if (value.lineStart) return String(value.lineStart);
  return value.line ? String(value.line) : '';
}

function registerCopy(key: string, value: string): void {
  if (value) copyPayloads.set(key, value);
}

async function copyText(key: string): Promise<void> {
  const value = copyPayloads.get(key);
  if (!value) { showToast('Nothing to copy.', true); return; }
  try {
    if (!navigator.clipboard) throw new Error('Clipboard API is not available.');
    await navigator.clipboard.writeText(value);
    showToast('Copied.');
  } catch (error) {
    showToast(`Copy failed: ${errorMessage(error)}`, true);
  }
}

function bind(id: string, handler: () => void | Promise<void>): void {
  getElement(id).addEventListener('click', () => void handler());
}

function getInput(id: string): HTMLInputElement {
  const element = document.getElementById(id);
  if (!(element instanceof HTMLInputElement)) throw new Error(`${id} input was not found.`);
  return element;
}

function getElement(id: string): HTMLElement {
  const element = document.getElementById(id);
  if (!element) throw new Error(`${id} element was not found.`);
  return element;
}

function showToast(message: string, isError = false): void {
  toast.textContent = message;
  toast.className = isError ? 'toast show error' : 'toast show';
  window.setTimeout(() => { toast.className = 'toast'; }, 2600);
}

function formatDate(value?: string): string {
  if (!value) return 'n/a';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#039;');
}
