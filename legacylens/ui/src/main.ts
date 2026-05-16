import './styles.css';

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
        <p class="muted">Flat, local-first review workflow powered by Git diffs, gpu-search-mcp, and optional local LLM summaries.</p>
      </div>
      <button id="statusButton" class="ghost">Check gpu-search</button>
    </section>

    <section class="grid">
      <div class="panel stack">
        <label for="repoPath">Repository path</label>
        <input id="repoPath" placeholder="D:\\\\Projects\\\\ExampleRepo" />
        <label class="check"><input id="useLlm" type="checkbox" /> Include local LLM summary</label>
        <div class="actions">
          <button id="previewButton">Preview diff</button>
          <button id="reviewButton">Run review</button>
          <button id="analyzeButton" class="secondary">.NET analysis</button>
        </div>
        <details class="audit-options">
          <summary>Legacy Audit</summary>
          <div class="stack audit-form">
            <label class="check"><input id="auditUseLlm" type="checkbox" /> Use LLM summary</label>
            <label class="check"><input id="auditIncludeRoslyn" type="checkbox" checked /> Include Roslyn</label>
            <label class="check"><input id="auditIncludeGpuSearch" type="checkbox" checked /> Include gpu-search</label>
            <label class="check"><input id="auditIncludePresets" type="checkbox" checked /> Include .NET presets</label>
            <label class="check"><input id="auditIncludeDi" type="checkbox" checked /> Include DI analysis</label>
            <button id="auditButton" class="secondary">Run legacy audit</button>
          </div>
        </details>
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
        <h2>Report viewer</h2>
        <span id="statusPill" class="pill">Ready</span>
      </div>
      <div id="toast" class="toast" aria-live="polite"></div>
      <div id="output" class="output muted">Run a preview, review, or analysis to see results.</div>
    </section>
  </main>
`;

const repoPath = getInput('repoPath');
const useLlm = getInput('useLlm') as HTMLInputElement;
const output = getElement('output');
const statusPill = getElement('statusPill');
const reports = getElement('reports');
const toast = getElement('toast');
const copyPayloads = new Map<string, string>();

bind('statusButton', checkStatus);
bind('previewButton', previewDiff);
bind('reviewButton', runReview);
bind('analyzeButton', runAnalysis);
bind('auditButton', runAudit);
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
      ${card(`${preview.changedFileCount ?? 0} changed file(s)`, (preview.files ?? []).map(formatFile))}
      <pre>${escapeHtml(preview.diff || 'No diff content.')}</pre>
    `;
  });
}

async function runReview(): Promise<void> {
  await run('Generating review...', async () => {
    const report = await api<ReviewReport>('/api/review/diff', {
      method: 'POST',
      body: JSON.stringify({ repoPath: repoPath.value, useLlm: useLlm.checked }),
    });
    renderReport(report);
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
      `${analysis.findings?.length ?? 0} preset finding(s)`,
      (analysis.findings ?? []).map(
        (finding) =>
          `${finding.presetId ?? 'preset'} · ${finding.filePath ?? 'unknown file'}${finding.line ? `:${finding.line}` : ''} · ${finding.snippet ?? ''}`,
      ),
    );
  });
}

async function runAudit(): Promise<void> {
  await run('Running legacy audit...', async () => {
    const auditUseLlm = document.getElementById('auditUseLlm') as HTMLInputElement | null;
    const auditIncludeRoslyn = document.getElementById('auditIncludeRoslyn') as HTMLInputElement | null;
    const auditIncludeGpuSearch = document.getElementById('auditIncludeGpuSearch') as HTMLInputElement | null;
    const auditIncludePresets = document.getElementById('auditIncludePresets') as HTMLInputElement | null;
    const auditIncludeDi = document.getElementById('auditIncludeDi') as HTMLInputElement | null;

    const report = await api<LegacyAuditReport>('/api/audit/legacy-dotnet', {
      method: 'POST',
      body: JSON.stringify({
        repoPath: repoPath.value,
        useLlm: auditUseLlm?.checked ?? false,
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

function renderAuditReport(report: LegacyAuditReport): void {
  copyPayloads.clear();
  const markdown = report.markdown ?? '';
  registerCopy('audit-markdown', markdown);

  output.innerHTML = `
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Legacy .NET Audit Report</p>
          <h2>${escapeHtml(report.reportId ?? 'Audit report')}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="audit-markdown">Copy Markdown</button>
        </div>
      </header>
      ${renderAuditSummary(report)}
      ${renderAuditWorkspace(report)}
      ${renderTechnologySignals(report.technologySignals ?? [])}
      ${renderArchitectureSignals(report.architectureSignals ?? [])}
      ${renderAuditFindings(report.riskFindings ?? [])}
      ${renderAuditRoslynSummary(report.roslynSummary ?? null)}
      ${renderAuditDiSummary(report.dependencyInjectionSummary ?? null)}
      ${renderAuditGpuSearchSummary(report.gpuSearchSummary ?? null)}
      ${renderNextSteps(report.recommendedNextSteps ?? [])}
      ${report.llmSummary ? renderSection('LLM Summary', `<div class="llm-summary">${escapeHtml(report.llmSummary)}</div>`) : ''}
      ${renderRawMarkdown(markdown)}
    </article>
  `;

  output.querySelectorAll<HTMLButtonElement>('[data-copy]').forEach((button) => {
    button.addEventListener('click', () => copyText(button.dataset.copy ?? ''));
  });
}

function renderAuditSummary(report: LegacyAuditReport): string {
  return renderSection(
    'Summary',
    `
      <div class="meta-grid">
        ${meta('Repository', report.repoPath)}
        ${meta('Generated', report.generatedAtUtc ? formatDate(report.generatedAtUtc) : undefined)}
        ${meta('Technology signals', String(report.technologySignals?.length ?? 0))}
        ${meta('Risk findings', String(report.riskFindings?.length ?? 0))}
      </div>
      <p>${escapeHtml(report.summary ?? '')}</p>
    `,
  );
}

function renderAuditWorkspace(report: LegacyAuditReport): string {
  const ws = report.workspaceSummary;
  if (!ws) return '';

  return renderSection(
    'Workspace',
    `
      <div class="meta-grid">
        ${meta('Selected workspace', ws.selectedWorkspacePath ?? 'None')}
        ${meta('Kind', ws.selectedWorkspaceKind ?? 'n/a')}
        ${meta('Candidates', `${ws.totalCandidates ?? 0} (${ws.slnxCount ?? 0} .slnx, ${ws.slnCount ?? 0} .sln, ${ws.csprojCount ?? 0} .csproj)`)}
      </div>
      ${ws.warnings?.length ? detailsList('Warnings', ws.warnings) : ''}
    `,
  );
}

function renderTechnologySignals(signals: TechnologySignal[]): string {
  if (signals.length === 0) {
    return renderSection('Technology Signals', '<p class="empty">No signals detected.</p>');
  }

  const items = signals.map((s) => `
    <article class="finding-card">
      <div class="row">
        <span class="badge">${escapeHtml(s.category ?? 'Signal')}</span>
        <span class="badge confidence">${escapeHtml(s.confidence ?? 'unknown')}</span>
      </div>
      <h3>${escapeHtml(s.name ?? 'Signal')}</h3>
      ${s.filePath ? `<p class="mono muted">${escapeHtml(s.filePath)}</p>` : ''}
      <p class="muted">${escapeHtml(s.evidence ?? '')}</p>
    </article>
  `).join('');

  return renderSection('Technology Signals', `<div class="finding-list">${items}</div>`);
}

function renderArchitectureSignals(signals: ArchitectureSignal[]): string {
  if (signals.length === 0) {
    return renderSection('Architecture Signals', '<p class="empty">No signals detected.</p>');
  }

  const items = signals.map((s) => `
    <article class="finding-card">
      <div class="row">
        <span class="badge confidence">${escapeHtml(s.confidence ?? 'unknown')}</span>
      </div>
      <h3>${escapeHtml(s.name ?? 'Signal')}</h3>
      <p>${escapeHtml(s.message ?? '')}</p>
      <p class="muted">${escapeHtml(s.evidence ?? '')}</p>
    </article>
  `).join('');

  return renderSection('Architecture Signals', `<div class="finding-list">${items}</div>`);
}

function renderAuditFindings(findings: AuditFinding[]): string {
  if (findings.length === 0) {
    return renderSection('Risk Findings', '<p class="empty">No risk findings.</p>');
  }

  const items = findings.map((f) => {
    const severity = normalizeSeverity(f.severity);
    return `
      <article class="finding-card">
        <div class="row">
          <span class="badge severity-${severity.toLowerCase()}">${severity}</span>
          ${f.code ? `<span class="muted mono">${escapeHtml(f.code)}</span>` : ''}
        </div>
        <h3>${escapeHtml(f.title ?? f.code ?? 'Finding')}</h3>
        ${f.filePath ? `<p class="mono muted">${escapeHtml(f.filePath)}${f.line ? `:${f.line}` : ''}</p>` : ''}
        <p>${escapeHtml(f.message ?? '')}</p>
        ${f.evidence ? `<p class="muted"><em>Evidence: ${escapeHtml(f.evidence)}</em></p>` : ''}
      </article>
    `;
  }).join('');

  return renderSection('Risk Findings', `<div class="finding-list">${items}</div>`);
}

function renderAuditRoslynSummary(roslyn: AuditRoslynSummary | null): string {
  if (!roslyn) {
    return renderSection('Roslyn Summary', '<p class="empty">Roslyn analysis was not requested.</p>');
  }

  if (!roslyn.workspaceLoaded) {
    return renderSection(
      'Roslyn Summary',
      `<p class="error">Roslyn workspace could not be loaded.</p>
       ${roslyn.errorMessage ? `<p class="muted">${escapeHtml(roslyn.errorMessage)}</p>` : ''}
       ${roslyn.warnings?.length ? detailsList('Warnings', roslyn.warnings) : ''}`,
    );
  }

  return renderSection(
    'Roslyn Summary',
    `
      <div class="meta-grid">
        ${meta('Workspace', roslyn.workspacePath ?? 'n/a')}
        ${meta('Kind', roslyn.workspaceKind ?? 'n/a')}
        ${meta('Projects', String(roslyn.projectCount ?? 0))}
        ${meta('Documents', String(roslyn.documentCount ?? 0))}
        ${meta('Symbols', String(roslyn.symbolCount ?? 0))}
        ${meta('Classes', String(roslyn.classCount ?? 0))}
        ${meta('Interfaces', String(roslyn.interfaceCount ?? 0))}
        ${meta('Methods', String(roslyn.methodCount ?? 0))}
      </div>
      ${roslyn.warnings?.length ? detailsList('Warnings', roslyn.warnings) : ''}
    `,
  );
}

function renderAuditDiSummary(di: DiSummary | null): string {
  if (!di) {
    return renderSection('Dependency Injection Summary', '<p class="empty">DI analysis was not requested.</p>');
  }

  const lifetimes = di.registrationsByLifetime
    ? Object.entries(di.registrationsByLifetime).map(([k, v]) => `${k}: ${v}`).join(', ')
    : 'n/a';

  const diFindings = (di.findings ?? []).map((f) => `
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${normalizeSeverity(f.severity).toLowerCase()}">${normalizeSeverity(f.severity)}</span>
        ${f.code ? `<span class="muted mono">${escapeHtml(f.code)}</span>` : ''}
      </div>
      <p>${escapeHtml(f.message ?? '')}</p>
      ${f.filePath ? `<p class="mono muted">${escapeHtml(f.filePath)}${f.line ? `:${f.line}` : ''}</p>` : ''}
    </article>
  `).join('');

  return renderSection(
    'Dependency Injection Summary',
    `
      <div class="meta-grid">
        ${meta('Registrations', String(di.registrationCount ?? 0))}
        ${meta('Constructor deps', String(di.constructorDependencyCount ?? 0))}
        ${meta('Findings', String(di.findingCount ?? 0))}
        ${meta('By lifetime', lifetimes)}
      </div>
      ${diFindings ? `<div class="finding-list">${diFindings}</div>` : ''}
    `,
  );
}

function renderAuditGpuSearchSummary(gpuSearch: GpuSearchAuditSummary | null): string {
  if (!gpuSearch) {
    return renderSection('gpu-search Signal Scan', '<p class="empty">gpu-search was not requested.</p>');
  }

  if (!gpuSearch.wasAvailable) {
    return renderSection(
      'gpu-search Signal Scan',
      `<p class="error">gpu-search was unavailable.</p>
       ${gpuSearch.errorMessage ? `<p class="muted">${escapeHtml(gpuSearch.errorMessage)}</p>` : ''}`,
    );
  }

  const modeBadge = gpuSearch.usedSignalScan
    ? '<span class="badge badge-success">Signal Scan</span>'
    : '<span class="badge badge-warning">Fallback: Individual Queries</span>';

  const categoryPills = gpuSearch.usedSignalScan && (gpuSearch.signalCategories?.length ?? 0) > 0
    ? `<div class="tag-list">${(gpuSearch.signalCategories ?? []).map((c) => `<span class="tag">${escapeHtml(c)}</span>`).join('')}</div>`
    : '';

  const scanWarnings = (gpuSearch.scanWarnings ?? []).map((w) => `<p class="warning">⚠ ${escapeHtml(w)}</p>`).join('');
  const scanLimitations = (gpuSearch.scanLimitations ?? []).map((l) => `<li>${escapeHtml(l)}</li>`).join('');

  const countLabel = gpuSearch.usedSignalScan ? 'Signals scanned' : 'Queries run';

  const resultItems = (gpuSearch.results ?? []).slice(0, 20).map((r) => `
    <article class="context-card">
      <div class="row">
        <span class="muted mono">${escapeHtml(r.query ?? '')}</span>
        ${r.filePath ? `<span class="mono">${escapeHtml(r.filePath)}${r.line ? `:${r.line}` : ''}</span>` : ''}
      </div>
      ${r.snippet ? `<pre>${escapeHtml(r.snippet)}</pre>` : ''}
    </article>
  `).join('');

  return renderSection(
    'gpu-search Signal Scan',
    `
      <div class="row">${modeBadge}</div>
      <p class="muted"><em>Results are heuristic/retrieval-based, not compiler-verified.</em></p>
      ${categoryPills}
      <div class="meta-grid">
        ${meta(countLabel, String(gpuSearch.queriesRun ?? 0))}
        ${meta('Total matches', String(gpuSearch.totalResults ?? 0))}
      </div>
      ${scanWarnings}
      ${scanLimitations ? `<ul class="muted">${scanLimitations}</ul>` : ''}
      ${resultItems ? `<div class="context-list">${resultItems}</div>` : '<p class="empty">No results returned.</p>'}
    `,
  );
}

function renderNextSteps(steps: string[]): string {
  if (steps.length === 0) {
    return renderSection('Recommended Next Steps', '<p class="empty">No recommendations generated.</p>');
  }

  return renderSection(
    'Recommended Next Steps',
    `<ul>${steps.map((s) => `<li>${escapeHtml(s)}</li>`).join('')}</ul>`,
  );
}

async function loadReports(): Promise<void> {
  reports.innerHTML = '<p class="muted">Loading saved reports...</p>';
  try {
    const items = await api<ReportSummary[]>('/api/reports');
    reports.innerHTML = items.length
      ? items.map(reportItem).join('')
      : '<p class="empty">No saved reports yet. Run a review or audit to create one.</p>';
    reports.querySelectorAll<HTMLButtonElement>('[data-view]').forEach((button) => {
      button.addEventListener('click', () => viewReport(button.dataset.view ?? '', button.dataset.viewType));
    });
    reports.querySelectorAll<HTMLButtonElement>('[data-delete]').forEach((button) => {
      button.addEventListener('click', () => deleteReport(button.dataset.delete ?? ''));
    });
  } catch (error) {
    reports.innerHTML = `<p class="error">Could not load saved reports: ${escapeHtml(errorMessage(error))}</p>`;
  }
}

async function viewReport(id: string, reportType?: string): Promise<void> {
  await run('Loading report...', async () => {
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
  if (!window.confirm('Delete this saved report?')) {
    return;
  }

  await run('Deleting report...', async () => {
    await api(`/api/reports/${encodeURIComponent(id)}`, { method: 'DELETE' });
    await loadReports();
    showToast('Report deleted.');
  });
}

function renderReport(report: ReviewReport): void {
  copyPayloads.clear();
  const markdown = report.markdown ?? '';
  const context = report.gpuSearchContext ?? null;
  const roslynCtx = report.roslynContext ?? null;
  const llmSummary = report.llmSummary ?? '';

  registerCopy('markdown', markdown);
  if (context) {
    registerCopy('context', JSON.stringify(context, null, 2));
  }
  if (llmSummary) {
    registerCopy('llm', llmSummary);
  }

  output.innerHTML = `
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Review report</p>
          <h2>${escapeHtml(reportTitle(report))}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="markdown">Copy Markdown</button>
          ${context ? '<button class="ghost small" data-copy="context">Copy gpu-search context</button>' : ''}
          ${llmSummary ? '<button class="ghost small" data-copy="llm">Copy LLM summary</button>' : ''}
        </div>
      </header>
      ${renderSummary(report)}
      ${renderFindings(report.findings ?? [])}
      ${renderGpuSearchContext(context)}
      ${renderRoslynContext(roslynCtx)}
      ${llmSummary ? renderSection('LLM Summary', `<div class="llm-summary">${escapeHtml(llmSummary)}</div>`) : ''}
      ${renderRawMarkdown(markdown)}
    </article>
  `;

  output.querySelectorAll<HTMLButtonElement>('[data-copy]').forEach((button) => {
    button.addEventListener('click', () => copyText(button.dataset.copy ?? ''));
  });
}

function renderSummary(report: ReviewReport): string {
  const generated = report.generatedAtUtc ?? report.createdAtUtc ?? report.createdAt;
  return renderSection(
    'Summary',
    `
      <div class="meta-grid">
        ${meta('Repository', report.repoPath)}
        ${meta('Created', generated ? formatDate(generated) : undefined)}
        ${meta('Changed files', String(report.changedFileCount ?? report.files?.length ?? 'n/a'))}
        ${meta('Provider / mode', report.providerName ?? report.llmProvider ?? report.reviewMode ?? report.analysisMode)}
      </div>
    `,
  );
}

function renderFindings(findings: ReviewFinding[]): string {
  if (findings.length === 0) {
    return renderSection('Findings', '<p class="empty">No findings returned.</p>');
  }

  return renderSection(
    'Findings',
    `<div class="finding-list">${findings.map(renderFinding).join('')}</div>`,
  );
}

function renderFinding(finding: ReviewFinding): string {
  const severity = normalizeSeverity(finding.severity);
  const location = finding.path ?? finding.filePath;
  const line = formatLineRange(finding);
  return `
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${severity.toLowerCase()}">${severity}</span>
        ${finding.category ? `<span class="badge">${escapeHtml(finding.category)}</span>` : ''}
        ${finding.ruleId ? `<span class="muted mono">${escapeHtml(finding.ruleId)}</span>` : ''}
      </div>
      <h3>${escapeHtml(finding.title ?? finding.message ?? 'Review finding')}</h3>
      ${location ? `<p class="mono muted">${escapeHtml(location)}${line ? `:${escapeHtml(line)}` : ''}</p>` : ''}
      ${finding.description ? `<p>${escapeHtml(finding.description)}</p>` : ''}
    </article>
  `;
}

function renderGpuSearchContext(context: GpuSearchContext | null): string {
  if (!context) {
    return renderSection('gpu-search Context', '<p class="empty">No gpu-search context was included in this report.</p>');
  }

  if (context.wasAvailable === false) {
    return renderSection(
      'gpu-search Context',
      `<p class="error">gpu-search unavailable: ${escapeHtml(context.unavailableReason ?? 'No reason provided.')}</p>`,
    );
  }

  const warnings = context.warnings?.length ? detailsList('Global warnings', context.warnings) : '';
  const limitations = context.limitations?.length ? detailsList('Global limitations', context.limitations) : '';
  const files = context.files?.length
    ? context.files.map(renderChangedFileContext).join('')
    : '<p class="empty">No per-file gpu-search context returned.</p>';

  return renderSection('gpu-search Context', `${warnings}${limitations}<div class="context-list">${files}</div>`);
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
      <div class="row">
        <h3 class="mono">${escapeHtml(file.filePath ?? file.path ?? 'Unknown file')}</h3>
        <div class="actions compact">
          ${badge(impact?.confidence ?? file.confidence, 'confidence')}
          ${badge(impact?.analysisMode ?? file.analysisMode, 'mode')}
        </div>
      </div>
      ${file.error ? `<p class="error">${escapeHtml(file.error)}</p>` : ''}
      <p class="muted">${escapeHtml(impact?.summary ?? dependencySummary(impact, impactedFiles))}</p>
      ${impactedFiles.length ? impactedFilesBlock(impactedFiles) : ''}
      ${warnings.length ? detailsList('Warnings', warnings) : ''}
      ${limitations.length ? detailsList('Limitations', limitations) : ''}
      ${renderRelatedResults(file.relatedResults ?? [])}
      ${
        skeletonContent
          ? `<details><summary>Skeleton preview${typeof skeleton === 'object' && skeleton?.language ? ` · ${escapeHtml(skeleton.language)}` : ''}</summary><pre>${escapeHtml(skeletonContent)}</pre></details>`
          : ''
      }
    </article>
  `;
}

function renderRelatedResults(results: RelatedCodeResult[]): string {
  if (results.length === 0) {
    return '<p class="muted">No related search results.</p>';
  }

  return `
    <div class="related-list">
      <h4>Related search results</h4>
      ${results
        .map(
          (result) => `
            <article class="related-card">
              <div class="row">
                <strong class="mono">${escapeHtml(result.file ?? result.filePath ?? 'Unknown file')}${formatLineRange(result) ? `:${escapeHtml(formatLineRange(result))}` : ''}</strong>
                <span class="badge">${escapeHtml(result.engine ?? 'search')}${result.score !== null && result.score !== undefined ? ` · ${result.score.toFixed(2)}` : ''}</span>
              </div>
              ${result.reason ? `<p class="muted">${escapeHtml(result.reason)}</p>` : ''}
              ${result.snippet ? `<pre>${escapeHtml(result.snippet)}</pre>` : ''}
            </article>
          `,
        )
        .join('')}
    </div>
  `;
}

function renderRoslynContext(context: RoslynReviewContext | null): string {
  if (!context) {
    return '';
  }

  if (!context.success || (!context.changedSymbols?.length && context.errorMessage)) {
    return renderSection(
      'Roslyn Reference Context',
      `<p class="error">Roslyn reference analysis was unavailable. Review continued with deterministic and gpu-search context.</p>
       ${context.errorMessage ? `<p class="muted">${escapeHtml(context.errorMessage)}</p>` : ''}`,
    );
  }

  const symbols = context.changedSymbols ?? [];
  if (symbols.length === 0) {
    return renderSection('Roslyn Reference Context', '<p class="empty">No changed C# symbols were matched.</p>');
  }

  const allRefs = context.symbolReferences ?? [];
  const symbolCards = symbols
    .map((symbol) => {
      const refs = allRefs.filter(
        (r) => r.symbolName === symbol.name && r.symbolFullName === symbol.fullName && !r.isDefinition,
      );
      const refRows = refs
        .slice(0, 10)
        .map((r) => {
          const container = r.containingSymbol ? ` <span class="muted">— in ${escapeHtml(r.containingSymbol)}</span>` : '';
          return `<li class="mono">${escapeHtml(r.filePath ?? '')}:${r.line ?? '?'}${container}</li>`;
        })
        .join('');

      return `
        <article class="context-card">
          <div class="row">
            <h3 class="mono">${escapeHtml(symbol.name ?? 'Unknown')}</h3>
            <span class="badge">${escapeHtml(symbol.kind ?? 'symbol')}</span>
          </div>
          <div class="meta-grid">
            ${meta('Project', symbol.projectName)}
            ${meta('Definition', symbol.filePath ? `${symbol.filePath}:${symbol.line ?? '?'}` : undefined)}
            ${meta('References', String(refs.length))}
          </div>
          ${refs.length ? `<div class="mini-block"><h4>References</h4><ul>${refRows}</ul></div>` : ''}
        </article>
      `;
    })
    .join('');

  const warnings = context.warnings?.length ? detailsList('Warnings', context.warnings) : '';
  const workspaceInfo =
    context.workspacePath
      ? `<p class="muted mono">${escapeHtml(context.workspacePath)}${context.workspaceKind ? ` (${escapeHtml(context.workspaceKind)})` : ''}</p>`
      : '';

  return renderSection(
    'Roslyn Reference Context',
    `${workspaceInfo}${warnings}<div class="context-list">${symbolCards}</div>`,
  );
}

function renderRawMarkdown(markdown: string): string {
  return `
    <details class="raw-markdown">
      <summary>Raw Markdown</summary>
      <pre>${escapeHtml(markdown || 'No Markdown returned.')}</pre>
    </details>
  `;
}

async function run(label: string, action: () => Promise<void>): Promise<void> {
  setStatus(label);
  try {
    await action();
    setStatus('Ready');
  } catch (error) {
    setStatus('Error');
    showToast(errorMessage(error), true);
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
  const id = report.reportId ?? report.id ?? '';
  const type = report.reportType ?? 'DiffReview';
  const isAudit = type === 'LegacyAudit';
  const typeBadge = isAudit
    ? '<span class="badge badge-success" style="font-size:0.68rem">Legacy Audit</span>'
    : '<span class="badge" style="font-size:0.68rem">Diff Review</span>';
  const meta = isAudit
    ? `${formatDate(report.generatedAtUtc ?? report.createdAtUtc)} · ${escapeHtml(report.llmProvider ?? 'Deterministic')}`
    : `${formatDate(report.generatedAtUtc ?? report.createdAtUtc)} · ${report.changedFileCount ?? 'n/a'} files · ${escapeHtml(report.llmProvider ?? report.providerName ?? 'n/a')}`;
  return `
    <article class="report">
      <div>
        <strong>${escapeHtml(report.repoPath ?? 'Unknown repository')}</strong>
        <span>${typeBadge} ${meta}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${escapeHtml(id)}" data-view-type="${escapeHtml(type)}">View</button>
        <button class="danger small" data-delete="${escapeHtml(id)}">Delete</button>
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

function renderSection(title: string, content: string): string {
  return `
    <section class="viewer-section">
      <h2>${escapeHtml(title)}</h2>
      ${content}
    </section>
  `;
}

function meta(label: string, value?: string | null): string {
  return `
    <div class="meta-item">
      <span>${escapeHtml(label)}</span>
      <strong>${escapeHtml(value || 'n/a')}</strong>
    </div>
  `;
}

function listBlock(title: string, rows: string[]): string {
  return `
    <div class="mini-block">
      <h4>${escapeHtml(title)}</h4>
      <ul>${rows.map((row) => `<li class="mono">${escapeHtml(row)}</li>`).join('')}</ul>
    </div>
  `;
}

function impactedFilesBlock(files: ImpactedFile[]): string {
  return `
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${files
          .map((file) => {
            const path = file.file ?? file.filePath ?? file.path ?? 'Unknown file';
            const hops = file.hops !== null && file.hops !== undefined ? ` · ${file.hops} hop${file.hops === 1 ? '' : 's'}` : '';
            const reason = file.reason ? ` <span class="reason">— ${escapeHtml(file.reason)} <em>(heuristic)</em></span>` : '';
            return `<li class="mono">${escapeHtml(path)}${hops}${reason}</li>`;
          })
          .join('')}
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

function detailsList(title: string, rows: string[]): string {
  return `
    <details>
      <summary>${escapeHtml(title)} (${rows.length})</summary>
      <ul>${rows.map((row) => `<li>${escapeHtml(row)}</li>`).join('')}</ul>
    </details>
  `;
}

function badge(value?: string | null, className = ''): string {
  return value ? `<span class="badge ${className}">${escapeHtml(value)}</span>` : '';
}

function formatFile(file: GitDiffFile): string {
  return `${file.status ?? 'M'} ${file.path ?? file.filePath ?? 'Unknown file'} (+${file.additions ?? 0}/-${file.deletions ?? 0})`;
}

function dependencySummary(impact: DependencyImpactSummary | null | undefined, impactedFiles: string[]): string {
  if (!impact) {
    return 'No dependency impact summary returned.';
  }

  const total = impact.totalImpacted ?? impactedFiles.length;
  return `${total} impacted file(s) identified.`;
}

function reportTitle(report: ReviewReport): string {
  return report.reportId ?? report.id ?? 'Review report';
}

function formatLineRange(value: { line?: number | null; lineStart?: number | null; lineEnd?: number | null }): string {
  if (value.lineStart && value.lineEnd) {
    return value.lineStart === value.lineEnd ? String(value.lineStart) : `${value.lineStart}-${value.lineEnd}`;
  }

  if (value.lineStart) {
    return String(value.lineStart);
  }

  return value.line ? String(value.line) : '';
}

function normalizeSeverity(value?: string): string {
  const normalized = (value ?? 'Info').toLowerCase();
  if (['low', 'medium', 'high', 'critical'].includes(normalized)) {
    return normalized[0].toUpperCase() + normalized.slice(1);
  }

  return 'Info';
}

function registerCopy(key: string, value: string): void {
  if (value) {
    copyPayloads.set(key, value);
  }
}

async function copyText(key: string): Promise<void> {
  const value = copyPayloads.get(key);
  if (!value) {
    showToast('Nothing to copy.', true);
    return;
  }

  try {
    if (!navigator.clipboard) {
      throw new Error('Clipboard API is not available.');
    }

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

function showToast(message: string, isError = false): void {
  toast.textContent = message;
  toast.className = isError ? 'toast show error' : 'toast show';
  window.setTimeout(() => {
    toast.className = 'toast';
  }, 2600);
}

function formatDate(value?: string): string {
  if (!value) {
    return 'n/a';
  }

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
