(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const s of document.querySelectorAll('link[rel="modulepreload"]'))i(s);new MutationObserver(s=>{for(const r of s)if(r.type==="childList")for(const o of r.addedNodes)o.tagName==="LINK"&&o.rel==="modulepreload"&&i(o)}).observe(document,{childList:!0,subtree:!0});function n(s){const r={};return s.integrity&&(r.integrity=s.integrity),s.referrerPolicy&&(r.referrerPolicy=s.referrerPolicy),s.crossOrigin==="use-credentials"?r.credentials="include":s.crossOrigin==="anonymous"?r.credentials="omit":r.credentials="same-origin",r}function i(s){if(s.ep)return;s.ep=!0;const r=n(s);fetch(s.href,r)}})();const F=document.querySelector("#app");if(!F)throw new Error("App root was not found.");F.innerHTML=`
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
`;let P="audit",N="all",g=null,j=[],D=null;const x=new Map,m=Y("repoPath"),B=Y("useLlm"),I=b("toast");document.querySelectorAll(".tab[data-tab]").forEach(e=>{e.addEventListener("click",()=>X(e.dataset.tab??"audit"))});document.querySelectorAll("[data-rail-filter]").forEach(e=>{e.addEventListener("click",()=>Z(e.dataset.railFilter??"all"))});E("clearRepoPath",()=>{m.value="",m.focus()});E("previewButton",ee);E("runButton",q);E("refreshReportsButton",S);document.getElementById("chipGpu")?.addEventListener("click",()=>{U()});document.getElementById("chipOllama")?.addEventListener("click",()=>{z()});m.addEventListener("keydown",e=>{e.key==="Enter"&&q()});U();z();S();function X(e){P=e,document.querySelectorAll(".tab[data-tab]").forEach(o=>{o.classList.toggle("active",o.dataset.tab===e)});const t={audit:"LEGACY AUDIT",review:"DIFF REVIEW",analyze:".NET ANALYSIS"},n={audit:"Run audit",review:"Run review",analyze:"Run analysis"},i=document.getElementById("runTitle"),s=document.getElementById("runBreadcrumb"),r=document.getElementById("runButton");i&&(i.textContent=t[e]??e.toUpperCase()),s&&(s.textContent=`workflow / ${e} / new run`),r&&!r.disabled&&(r.innerHTML=`${n[e]??"Run"} <span class="kbd">↵</span>`)}function Z(e){N=e,document.querySelectorAll("[data-rail-filter]").forEach(t=>{t.classList.toggle("active",t.dataset.railFilter===e)}),O()}function O(){const e=document.getElementById("railItems");if(!e)return;const t=j.filter(n=>N==="audit"?n.reportType==="LegacyAudit":N==="review"?n.reportType!=="LegacyAudit":!0);if(t.length===0){e.innerHTML='<p style="padding:12px;color:var(--muted);font-size:var(--t-sm)">No reports match this filter.</p>';return}e.innerHTML=t.map(Se).join(""),e.querySelectorAll(".report-item[data-view]").forEach(n=>{n.addEventListener("click",()=>{g=n.dataset.view??null,document.querySelectorAll(".report-item").forEach(i=>{i.classList.toggle("active",i.dataset.view===g)}),se(n.dataset.view??"",n.dataset.viewType)})}),e.querySelectorAll("[data-delete]").forEach(n=>{n.addEventListener("click",i=>{i.stopPropagation(),ie(n.dataset.delete??"")})}),e.querySelectorAll("[data-export]").forEach(n=>{n.addEventListener("click",i=>{i.stopPropagation(),H(n.dataset.export??"",n.dataset.format??"")})})}async function U(){const e=document.getElementById("chipGpu"),t=document.getElementById("chipGpuLabel");try{const n=await v("/api/search/status");if(!e||!t)return;const i=n.isAvailable;e.className=i?"status-chip":"status-chip off";const s=n.stats?.indexedFileCount;t.textContent=i?`${n.stats?.device??"ready"}${s!=null?" · "+s.toLocaleString()+" files":""}`:n.error??"unavailable"}catch{e&&(e.className="status-chip off"),t&&(t.textContent="error")}}async function z(){try{D=await v("/api/llm/ollama/status"),_(D)}catch{const e=document.getElementById("chipOllama"),t=document.getElementById("chipOllamaLabel");e&&(e.className="status-chip off"),t&&(t.textContent="error")}}function _(e){const t=document.getElementById("chipOllama"),n=document.getElementById("chipOllamaLabel");if(!t||!n)return;const i=e.serverReachable&&e.modelInstalled,s=e.serverReachable&&!e.modelInstalled;t.className=i?"status-chip":s?"status-chip warn":"status-chip off";const r=e.configuredModel??"unknown";n.textContent=i?r:s?`${r} · not installed`:"offline"}async function q(){P==="review"?await te():P==="analyze"?await ne():await ae()}async function ee(){await w("Loading diff preview…",async()=>{const e=await v("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:m.value})}),t=b("reportArea");t.className="report-shell",t.innerHTML=`
      <div class="report-head">
        <div>
          <p class="report-eyebrow">Diff Preview</p>
          <h1 class="report-title">${a(C(m.value))}</h1>
          <div class="report-subtitle">${a(m.value)} · ${e.changedFileCount??0} changed file(s)</div>
        </div>
      </div>
      ${l("Changed files",e.changedFileCount??0,`
        <ul>
          ${(e.files??[]).map(n=>`<li style="font-family:var(--font-mono);font-size:var(--t-sm)">${a(n.status??"M")} ${a(n.path??n.filePath??"")} <span style="color:var(--muted)">(+${n.additions??0}/-${n.deletions??0})</span></li>`).join("")}
        </ul>
      `)}
      ${l("Diff",null,`<pre>${a(e.diff||"No diff content.")}</pre>`)}
    `})}async function te(){await w("Generating review…",async()=>{const e=await v("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:m.value,useLlm:B.checked})});W(e),await S()})}async function ne(){await w("Running .NET analysis…",async()=>{const e=await v("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:m.value,limitPerPreset:8})}),t=b("reportArea");t.className="report-shell";const n=e.findings??[],i=n.map(s=>`
      <tr>
        <td class="col-sev"><span class="badge ${R(s.severity)}">${A(s.severity??"")}</span></td>
        <td class="col-code">${a(s.presetId??"")}</td>
        <td class="col-title">
          <div>${a(s.filePath??"unknown file")}${s.line?`:${s.line}`:""}</div>
          ${s.snippet?`<div class="desc">${a(s.snippet)}</div>`:""}
        </td>
        <td class="col-loc">${a(s.rationale??"")}</td>
      </tr>
    `).join("");t.innerHTML=`
      <div class="report-head">
        <div>
          <p class="report-eyebrow">.NET Analysis</p>
          <h1 class="report-title">${a(C(m.value))}</h1>
          <div class="report-subtitle">${a(m.value)} · ${n.length} finding(s)</div>
        </div>
      </div>
      ${l("Findings",n.length,n.length?`<table class="finding-table"><thead><tr><th>Sev</th><th>Preset</th><th>Location</th><th>Note</th></tr></thead><tbody>${i}</tbody></table>`:'<p class="note">No findings returned.</p>')}
    `})}async function ae(){await w("Running legacy audit…",async()=>{const e=document.getElementById("auditIncludeRoslyn"),t=document.getElementById("auditIncludeGpuSearch"),n=document.getElementById("auditIncludePresets"),i=document.getElementById("auditIncludeDi"),s=await v("/api/audit/legacy-dotnet",{method:"POST",body:JSON.stringify({repoPath:m.value,useLlm:B.checked,includeRoslyn:e?.checked??!0,includeGpuSearch:t?.checked??!0,includeDotNetPresets:n?.checked??!0,includeDependencyInjection:i?.checked??!0})});G(s),await S()})}async function S(){try{j=await v("/api/reports"),O()}catch{const e=document.getElementById("railItems");e&&(e.innerHTML='<p style="padding:12px;color:var(--sev-high);font-size:var(--t-sm)">Could not load reports.</p>')}}async function se(e,t){await w("Loading report…",async()=>{if(t==="LegacyAudit"){const n=await v(`/api/audit/reports/${encodeURIComponent(e)}`);G(n)}else{const n=await v(`/api/reports/${encodeURIComponent(e)}`);W(n)}})}async function ie(e){window.confirm("Delete this saved report?")&&await w("Deleting report…",async()=>{if(await v(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),g===e){g=null;const t=b("reportArea");t.className="empty-state",t.innerHTML="<p>Run an audit, review, or analysis to see results.</p>"}await S(),y("Report deleted.")})}async function H(e,t){if(!e||t!=="markdown"&&t!=="html"){y("Export is unavailable for this report.",!0);return}await w(`Exporting ${t}…`,async()=>{const n=await fetch(`/api/audit/reports/${encodeURIComponent(e)}/export/${t}`);if(!n.ok){const c=await n.text();throw new Error(c||`${n.status} ${n.statusText}`)}const i=await n.blob(),s=oe(n.headers.get("content-disposition"))??`legacy-audit-${e}.${t==="markdown"?"md":"html"}`,r=URL.createObjectURL(i),o=document.createElement("a");o.href=r,o.download=s,document.body.appendChild(o),o.click(),o.remove(),URL.revokeObjectURL(r),y(`Exported ${s}.`)})}function oe(e){if(!e)return null;const t=/filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(e);return t?decodeURIComponent(t[1].replaceAll('"',"").trim()):null}function G(e){x.clear();const t=e.markdown??"";L("audit-markdown",t),g=e.reportId??null;const n=b("reportArea");n.className="report-shell",n.innerHTML=`
    ${re(e)}
    ${le(e)}
    ${ce(e)}
    ${de(e.technologySignals??[])}
    ${ue(e.architectureSignals??[])}
    ${pe(e.riskFindings??[])}
    ${me(e.roslynSummary??null)}
    ${ve(e.dependencyInjectionSummary??null)}
    ${ge(e.gpuSearchSummary??null)}
    ${he(e.recommendedNextSteps??[])}
    ${e.llmSummary?l("LLM Summary",null,`<div class="llm-summary">${a(e.llmSummary)}</div>`):""}
    ${t?K(t):""}
  `,n.querySelectorAll("[data-copy]").forEach(i=>{i.addEventListener("click",()=>{Q(i.dataset.copy??"")})}),n.querySelectorAll("[data-export]").forEach(i=>{i.addEventListener("click",()=>{H(e.reportId??"",i.dataset.format??"")})}),document.querySelectorAll(".report-item").forEach(i=>{i.classList.toggle("active",i.dataset.view===g)})}function re(e){const t=e.reportId??"",n=e.generatedAtUtc?J(e.generatedAtUtc):"—";return`
    <div class="report-head">
      <div>
        <p class="report-eyebrow">Legacy .NET Audit · Report</p>
        <h1 class="report-title">${a(C(e.repoPath))}</h1>
        <div class="report-subtitle">${a(e.repoPath??"")} · id <code>${a(t.slice(0,8))}</code> · generated ${a(n)}</div>
      </div>
      <div class="report-actions">
        <button class="btn ghost sm" data-copy="audit-markdown">Copy Markdown</button>
        <button class="btn ghost sm" data-export data-format="html">Export HTML</button>
        <button class="btn ghost sm" data-export data-format="markdown">Export MD</button>
      </div>
    </div>
  `}function le(e){const t=e.metrics,n=t?.technologySignals??e.technologySignals?.length??0,i=t?.riskFindings??e.riskFindings?.length??0,s=e.riskFindings??[],r=t?.highCount??s.filter(k=>{const h=(k.severity??"").toLowerCase();return h==="high"||h==="critical"}).length,o=t?.warningCount??s.filter(k=>{const h=(k.severity??"").toLowerCase();return h==="warning"||h==="medium"}).length,c=t?.infoCount??s.filter(k=>(k.severity??"").toLowerCase()==="info").length,u=t?.gpuMatches??e.gpuSearchSummary?.totalResults??0,p=e.gpuSearchSummary?.queriesRun??0,f=t?.durationMs;return`
    <div class="metric-strip">
      <div class="metric">
        <div class="label">Repository</div>
        <div class="value" style="font-size:14px;font-family:var(--font-mono);letter-spacing:0">${a(C(e.repoPath))}</div>
      </div>
      <div class="metric info">
        <div class="label">Tech signals</div>
        <div class="value">${n}<span class="sub">detected</span></div>
      </div>
      <div class="metric ${i>0&&r>0?"bad":i>0?"warn":""}">
        <div class="label">Risk findings</div>
        <div class="value">${i}<span class="sub">${r}H · ${o}W · ${c}I</span></div>
      </div>
      <div class="metric">
        <div class="label">gpu-search matches</div>
        <div class="value">${u}<span class="sub">${p} queries</span></div>
      </div>
      <div class="metric">
        <div class="label">Duration</div>
        <div class="value">${f!=null?(f/1e3).toFixed(1):"—"}<span class="sub">${f!=null?"seconds":""}</span></div>
      </div>
    </div>
  `}function ce(e){const t=e.workspaceSummary,n=t?.selectedWorkspaceKind?`workspace · ${t.selectedWorkspaceKind}`:void 0,i=t?`
    <div class="callout" style="margin-top:14px">
      <div class="callout-icon">i</div>
      <div>
        <strong>Workspace resolved:</strong>
        <span style="font-family:var(--font-mono);font-size:var(--t-sm);margin-left:6px">${a(t.selectedWorkspacePath??"n/a")}</span>
        <span style="color:var(--muted);margin-left:8px;font-size:var(--t-sm)">(${t.totalCandidates??0} candidates · ${t.slnCount??0} .sln · ${t.csprojCount??0} .csproj)</span>
      </div>
    </div>
  `:"";return l("Summary",null,`
    <p class="prose" style="margin-top:0">${a(e.summary??"")}</p>
    ${i}
  `,n)}function de(e){if(e.length===0)return l("Technology signals",0,'<p class="note">No signals detected.</p>');const t=e.map(n=>`
    <article class="signal">
      <div class="signal-icon">${a(xe(n.category??""))}</div>
      <div class="signal-body">
        <div class="signal-meta">
          <span class="badge category">${a(n.category??"Signal")}</span>
          <span class="badge confidence">conf · ${a(n.confidence??"unknown")}</span>
        </div>
        <p class="signal-title">${a(n.name??"Signal")}</p>
        ${n.filePath?`<p class="signal-path">${a(n.filePath)}</p>`:""}
        <p class="signal-evidence">${a(n.evidence??"")}</p>
      </div>
    </article>
  `).join("");return l("Technology signals",e.length,`<div class="signal-grid">${t}</div>`)}function ue(e){if(e.length===0)return"";const t=e.map(n=>`
    <div style="display:grid;grid-template-columns:auto 1fr;gap:16px;align-items:start;padding:4px 0">
      <span class="badge confidence">conf · ${a(n.confidence??"unknown")}</span>
      <div>
        <p style="margin:0 0 4px;font-size:var(--t-base);font-weight:500">${a(n.name??"")}</p>
        <p class="prose" style="margin:0 0 4px">${a(n.message??"")}</p>
        <p style="margin:0;font-family:var(--font-mono);font-size:var(--t-xs);color:var(--muted)">${a(n.evidence??"")}</p>
      </div>
    </div>
  `).join("");return l("Architecture signals",e.length,t)}function pe(e){if(e.length===0)return l("Risk findings",0,'<p class="note">No risk findings.</p>');const t=e.filter(o=>{const c=(o.severity??"").toLowerCase();return c==="high"||c==="critical"}).length,n=e.filter(o=>{const c=(o.severity??"").toLowerCase();return c==="warning"||c==="medium"}).length,i=e.filter(o=>(o.severity??"").toLowerCase()==="info").length,s=`${t} high · ${n} warning · ${i} info`,r=e.map(o=>`
    <tr>
      <td class="col-sev"><span class="badge ${R(o.severity)}">${A(o.severity??"")}</span></td>
      <td class="col-code">${a(o.code??"")}</td>
      <td class="col-title">
        <div>${a(o.title??o.code??"Finding")}</div>
        <div class="desc">${a(o.message??"")}</div>
      </td>
      <td class="col-loc">${o.filePath?a(o.filePath)+(o.line?`:${o.line}`:""):"—"}</td>
    </tr>
  `).join("");return l("Risk findings",e.length,`<table class="finding-table"><thead><tr><th>Severity</th><th>Rule</th><th>Finding</th><th>Location</th></tr></thead><tbody>${r}</tbody></table>`,s)}function me(e){if(!e)return l("Roslyn summary",null,'<p class="note">Roslyn analysis was not requested.</p>');const t=e.workspaceLoaded?"compiler-aware":"workspace load failed";return e.workspaceLoaded?l("Roslyn summary",null,`
    <div class="kv-grid">
      ${d("Workspace kind",e.workspaceKind??"n/a")}
      ${d("Projects",String(e.projectCount??0))}
      ${d("Documents",String(e.documentCount??0))}
      ${d("Symbols",String(e.symbolCount??0))}
      ${d("Classes",String(e.classCount??0))}
      ${d("Interfaces",String(e.interfaceCount??0))}
      ${d("Methods",String(e.methodCount??0))}
      ${d("Path",Le(e.workspacePath??"",36),!0)}
    </div>
    ${e.warnings?.length?$("Warnings",e.warnings):""}
  `,t):l("Roslyn summary",null,`
      <div class="callout warn">
        <div class="callout-icon">!</div>
        <div>
          <strong>Roslyn workspace could not be loaded.</strong>
          <div style="margin-top:4px;color:var(--muted);font-family:var(--font-mono);font-size:var(--t-xs)">Workspace · ${a(e.workspacePath??"n/a")}</div>
          ${e.errorMessage?`<div style="margin-top:6px;color:var(--ink-2)">${a(e.errorMessage)}</div>`:""}
        </div>
      </div>
      ${e.warnings?.length?$("Warnings",e.warnings):""}
    `,t)}function ve(e){if(!e)return l("Dependency injection summary",null,'<p class="note">DI analysis was not requested.</p>');const t=e.registrationsByLifetime??{},n=`S ${t.Singleton??0} · Sc ${t.Scoped??0} · T ${t.Transient??0}`,i=(e.findings??[]).map(s=>`
    <tr>
      <td class="col-sev"><span class="badge ${R(s.severity)}">${A(s.severity??"")}</span></td>
      <td class="col-code">${a(s.code??"")}</td>
      <td class="col-title"><div>${a(s.message??"")}</div></td>
      <td class="col-loc">${s.filePath?a(s.filePath)+(s.line?`:${s.line}`:""):"—"}</td>
    </tr>
  `).join("");return l("Dependency injection summary",null,`
    <div class="kv-grid">
      ${d("Registrations",String(e.registrationCount??0))}
      ${d("Constructor deps",String(e.constructorDependencyCount??0))}
      ${d("Findings",String(e.findingCount??0))}
      ${d("By lifetime",n,!0)}
    </div>
    ${e.findingCount===0?'<p class="note" style="margin-top:10px">No IServiceCollection registrations detected. Expected for .NET Framework projects without modern DI.</p>':""}
    ${i?`<table class="finding-table" style="margin-top:12px"><thead><tr><th>Sev</th><th>Rule</th><th>Finding</th><th>Location</th></tr></thead><tbody>${i}</tbody></table>`:""}
  `,"static · advisory")}function ge(e){if(!e)return l("gpu-search signal scan",null,'<p class="note">gpu-search was not requested.</p>');if(!e.wasAvailable)return l("gpu-search signal scan",null,`
      <div class="callout warn">
        <div class="callout-icon">!</div>
        <div>
          <strong>gpu-search was unavailable.</strong>
          ${e.errorMessage?`<div style="margin-top:6px;color:var(--ink-2)">${a(e.errorMessage)}</div>`:""}
        </div>
      </div>
    `);const t=`mode · ${e.usedSignalScan?"scan/signals":"fallback"} · ${e.queriesRun??0} queries`,n=(e.signalCategories??[]).map(s=>`<span class="cat-pill">${a(s)}</span>`).join(""),i=(e.results??[]).map(s=>`
    <div class="hit-row">
      <div class="hit-query">${a(s.query??"")}</div>
      <div class="hit-detail">
        <div class="hit-loc">${a(s.filePath??"")}${s.line!=null?`<span class="line">:${s.line}</span>`:""}</div>
        <div class="hit-snippet">${a(s.snippet??"")}</div>
      </div>
    </div>
  `).join("");return l("gpu-search signal scan",e.totalResults??0,`
    <div class="hits">
      ${n?`<div class="hits-cats">${n}</div>`:""}
      ${i}
    </div>
    <p class="note" style="margin-top:10px">Results are heuristic / retrieval-based and not compiler-verified. Showing top ${e.results?.length??0} of ${e.totalResults??0}.</p>
  `,t)}function he(e){return e.length===0?l("Recommended next steps",0,'<p class="note">No recommendations generated.</p>'):l("Recommended next steps",e.length,`<ol class="steps">${e.map(t=>`<li>${a(t)}</li>`).join("")}</ol>`)}function W(e){x.clear();const t=e.markdown??"",n=e.gpuSearchContext??null,i=e.roslynContext??null,s=e.llmSummary??"";L("markdown",t),n&&L("context",JSON.stringify(n,null,2)),s&&L("llm",s),g=e.reportId??e.id??null;const r=b("reportArea");r.className="report-shell";const o=e.reportId??e.id??"report",c=e.generatedAtUtc??e.createdAtUtc??e.createdAt;r.innerHTML=`
    <div class="report-head">
      <div>
        <p class="report-eyebrow">Diff Review · Report</p>
        <h1 class="report-title">${a(C(e.repoPath))}</h1>
        <div class="report-subtitle">${a(e.repoPath??"")} · id <code>${a(o.slice(0,8))}</code> · ${c?J(c):"—"}</div>
      </div>
      <div class="report-actions">
        <button class="btn ghost sm" data-copy="markdown">Copy Markdown</button>
        ${n?'<button class="btn ghost sm" data-copy="context">Copy gpu-search context</button>':""}
        ${s?'<button class="btn ghost sm" data-copy="llm">Copy LLM summary</button>':""}
      </div>
    </div>
    ${fe(e)}
    ${ye(e.findings??[])}
    ${$e(n)}
    ${ke(i)}
    ${s?l("LLM Summary",null,`<div class="llm-summary">${a(s)}</div>`):""}
    ${t?K(t):""}
  `,r.querySelectorAll("[data-copy]").forEach(u=>{u.addEventListener("click",()=>{Q(u.dataset.copy??"")})}),document.querySelectorAll(".report-item").forEach(u=>{u.classList.toggle("active",u.dataset.view===g)})}function fe(e){const t=e.generatedAtUtc??e.createdAtUtc??e.createdAt;return l("Summary",null,`
    <div class="kv-grid">
      ${d("Repository",e.repoPath??"n/a")}
      ${d("Created",t?Ie(t):"n/a")}
      ${d("Changed files",String(e.changedFileCount??e.files?.length??"n/a"))}
      ${d("Provider / mode",e.providerName??e.llmProvider??e.reviewMode??e.analysisMode??"n/a")}
    </div>
  `)}function ye(e){if(e.length===0)return l("Findings",0,'<p class="note">No findings returned.</p>');const t=e.filter(o=>{const c=(o.severity??"").toLowerCase();return c==="high"||c==="critical"}).length,n=e.filter(o=>{const c=(o.severity??"").toLowerCase();return c==="warning"||c==="medium"}).length,i=e.filter(o=>(o.severity??"").toLowerCase()==="info").length,s=`${t} high · ${n} warning · ${i} info`,r=e.map(o=>{const c=o.path??o.filePath,u=T(o);return`
      <tr>
        <td class="col-sev"><span class="badge ${R(o.severity)}">${A(o.severity??"")}</span></td>
        <td class="col-code">${a(o.ruleId??"")}${o.category?`<div style="margin-top:2px;color:var(--subtle)">${a(o.category)}</div>`:""}</td>
        <td class="col-title">
          <div>${a(o.title??o.message??"Finding")}</div>
          ${o.description?`<div class="desc">${a(o.description)}</div>`:""}
        </td>
        <td class="col-loc">${c?a(c)+(u?":"+a(u):""):"—"}</td>
      </tr>
    `}).join("");return l("Findings",e.length,`<table class="finding-table"><thead><tr><th>Severity</th><th>Rule</th><th>Finding</th><th>Location</th></tr></thead><tbody>${r}</tbody></table>`,s)}function $e(e){if(!e)return l("gpu-search Context",null,'<p class="note">No gpu-search context was included in this report.</p>');if(e.wasAvailable===!1)return l("gpu-search Context",null,`<div class="callout warn"><div class="callout-icon">!</div><div><strong>gpu-search unavailable:</strong> ${a(e.unavailableReason??"No reason provided.")}</div></div>`);const t=e.warnings?.length?$("Global warnings",e.warnings):"",n=e.limitations?.length?$("Global limitations",e.limitations):"",i=e.files?.length?e.files.map(we).join(""):'<p class="note">No per-file gpu-search context returned.</p>';return l("gpu-search Context",e.files?.length??null,`${t}${n}<div class="context-list">${i}</div>`)}function we(e){const t=e.dependencyImpact,n=Ae(t?.impactedFiles??e.impactedFiles,t?.directImporters),i=t?.warnings??e.warnings??[],s=t?.limitations??e.limitations??[],r=e.skeleton,o=typeof r=="string"?r:r?.content;return`
    <article class="context-card">
      <div style="display:flex;align-items:flex-start;justify-content:space-between;gap:8px;margin-bottom:6px">
        <h4 style="margin:0;font-family:var(--font-mono);font-size:var(--t-sm);color:var(--ink);word-break:break-all">${a(e.filePath??e.path??"Unknown file")}</h4>
        <div style="display:flex;gap:4px;flex-shrink:0">
          ${t?.confidence??e.confidence?`<span class="badge confidence">${a(t?.confidence??e.confidence??"")}</span>`:""}
          ${t?.analysisMode??e.analysisMode?`<span class="badge">${a(t?.analysisMode??e.analysisMode??"")}</span>`:""}
        </div>
      </div>
      ${e.error?`<p class="error" style="font-size:var(--t-sm)">${a(e.error)}</p>`:""}
      <p style="font-size:var(--t-sm);color:var(--muted);margin:0 0 8px">${a(t?.summary??Ee(t,n))}</p>
      ${n.length?Re(n):""}
      ${i.length?$("Warnings",i):""}
      ${s.length?$("Limitations",s):""}
      ${be(e.relatedResults??[])}
      ${o?`<details><summary>Skeleton preview${typeof r=="object"&&r?.language?` · ${a(r.language)}`:""}</summary><pre>${a(o)}</pre></details>`:""}
    </article>
  `}function be(e){return e.length===0?"":`
    <div class="related-list" style="margin-top:8px">
      <h4>Related search results</h4>
      ${e.map(t=>`
        <article class="related-card">
          <div style="display:flex;align-items:center;justify-content:space-between;gap:8px;margin-bottom:4px">
            <strong style="font-family:var(--font-mono);font-size:var(--t-sm)">${a(t.file??t.filePath??"Unknown")}${T(t)?`:${a(T(t))}`:""}</strong>
            <span class="badge">${a(t.engine??"search")}${t.score!=null?` · ${t.score.toFixed(2)}`:""}</span>
          </div>
          ${t.reason?`<p style="font-size:var(--t-xs);color:var(--muted);margin:0 0 4px">${a(t.reason)}</p>`:""}
          ${t.snippet?`<pre>${a(t.snippet)}</pre>`:""}
        </article>
      `).join("")}
    </div>
  `}function ke(e){if(!e)return"";if(!e.success||!e.changedSymbols?.length&&e.errorMessage)return l("Roslyn Reference Context",null,`
      <div class="callout warn">
        <div class="callout-icon">!</div>
        <div>
          <strong>Roslyn reference analysis was unavailable.</strong>
          ${e.errorMessage?`<div style="margin-top:6px;color:var(--ink-2)">${a(e.errorMessage)}</div>`:""}
        </div>
      </div>
    `);const t=e.changedSymbols??[];if(t.length===0)return l("Roslyn Reference Context",0,'<p class="note">No changed C# symbols were matched.</p>');const n=e.symbolReferences??[],i=t.map(o=>{const c=n.filter(p=>p.symbolName===o.name&&p.symbolFullName===o.fullName&&!p.isDefinition),u=c.slice(0,10).map(p=>{const f=p.containingSymbol?` <span style="color:var(--muted)">— in ${a(p.containingSymbol)}</span>`:"";return`<li style="font-family:var(--font-mono);font-size:var(--t-xs)">${a(p.filePath??"")}:${p.line??"?"}${f}</li>`}).join("");return`
      <article class="context-card">
        <div style="display:flex;align-items:center;gap:8px;margin-bottom:8px">
          <span style="font-family:var(--font-mono);font-size:var(--t-sm);font-weight:600">${a(o.name??"Unknown")}</span>
          <span class="badge">${a(o.kind??"symbol")}</span>
        </div>
        <div class="kv-grid">
          ${d("Project",o.projectName??"n/a")}
          ${d("Definition",o.filePath?`${o.filePath}:${o.line??"?"}`:"n/a")}
          ${d("References",String(c.length))}
        </div>
        ${c.length?`<div class="mini-block"><h4>References</h4><ul>${u}</ul></div>`:""}
      </article>
    `}).join(""),s=e.warnings?.length?$("Warnings",e.warnings):"",r=e.workspacePath?`<p style="font-family:var(--font-mono);font-size:var(--t-xs);color:var(--muted);margin:0 0 12px">${a(e.workspacePath)}${e.workspaceKind?` (${a(e.workspaceKind)})`:""}</p>`:"";return l("Roslyn Reference Context",t.length,`${r}${s}<div class="context-list">${i}</div>`)}function K(e){return`
    <div class="section">
      <details>
        <summary>Raw Markdown</summary>
        <pre style="margin-top:8px">${a(e||"No Markdown returned.")}</pre>
      </details>
    </div>
  `}function Se(e){const t=e.reportId??e.id??"",n=e.reportType??"DiffReview",i=n==="LegacyAudit",s=i?"audit":"review",r=i?"audit":"review",o=(()=>{const h=e.repoPath??"",M=h.replace(/\\/g,"/").split("/").filter(Boolean);return M.length>=2?M.slice(-2).join("\\"):h})(),c=e.generatedAtUtc??e.createdAtUtc??"",u=e.findingCount??e.findings,p=e.changedFileCount,f=e.llmProvider??e.providerName??"—";return`
    <div class="report-item ${t===g?"active":""}" data-view="${a(t)}" data-view-type="${a(n)}">
      <div class="item-top">
        <span class="item-type ${s}">${r}</span>
        <span class="item-time">${c?Ce(c):"—"}</span>
      </div>
      <div class="item-repo">${a(o)}</div>
      <div class="item-bot">
        ${u!=null?`<span>${u} findings</span><span class="dot-sep">·</span>`:""}
        ${p!=null?`<span>${p.toLocaleString()} files</span><span class="dot-sep">·</span>`:""}
        <span>llm · ${a(f)}</span>
        <span style="margin-left:auto;display:flex;gap:4px">
          ${i?`<button class="btn ghost sm" data-export="${a(t)}" data-format="html" style="padding:0 6px;height:20px;font-size:10px">HTML</button>`:""}
          <button class="btn danger sm" data-delete="${a(t)}" style="padding:0 6px;height:20px;font-size:10px">Del</button>
        </span>
      </div>
    </div>
  `}function l(e,t,n,i){const s=t!=null?`<span style="color:var(--subtle);margin-left:8px;font-weight:500">· ${t}</span>`:"",r=i?`<span class="head-meta">${a(i)}</span>`:"";return`
    <section class="section">
      <div class="section-head">
        <h3>${a(e)}${s}</h3>
        ${r}
      </div>
      ${n}
    </section>
  `}function d(e,t,n=!1){return`<div class="kv"><div class="label">${a(e)}</div><div class="value${n?" mono":""}">${a(t)}</div></div>`}function Ce(e){const t=Date.now()-new Date(e).getTime(),n=Math.floor(t/6e4);if(n<1)return"just now";if(n<60)return`${n}m ago`;const i=Math.floor(n/60);return i<24?`${i}h ago`:`${Math.floor(i/24)}d ago`}function J(e){return new Date(e).toLocaleString("en-US",{month:"short",day:"2-digit",hour:"2-digit",minute:"2-digit",hour12:!1})}function C(e){if(!e)return"Unknown repository";const t=e.replace(/\\/g,"/").split("/").filter(Boolean);return t[t.length-1]??e}function Le(e,t=60){return e?e.length<=t?e:"…"+e.slice(e.length-t):""}function xe(e){return{Framework:"FW",Dependencies:"PK",Configuration:"CF",Quality:"QA",Architecture:"AR",Security:"SE",Database:"DB"}[e]??(e?e.slice(0,2).toUpperCase():"··")}function R(e){const t=(e??"").toLowerCase();return t==="high"||t==="critical"?"sev-high":t==="warning"||t==="medium"?"sev-warning":t==="info"||t==="low"?"sev-info":t==="ok"||t==="success"?"sev-ok":"sev-info"}function A(e){return e?e.charAt(0).toUpperCase()+e.slice(1).toLowerCase():"—"}async function w(e,t){const n=document.getElementById("runButton"),i=n?.innerHTML??"";n&&(n.disabled=!0,n.textContent=e);try{await t()}catch(s){y(V(s),!0)}finally{n&&(n.disabled=!1,n.innerHTML=i)}}async function v(e,t){const n=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!n.ok){const i=await n.text();throw new Error(i||`${n.status} ${n.statusText}`)}if(n.status!==204)return await n.json()}function $(e,t){return`
    <details>
      <summary>${a(e)} (${t.length})</summary>
      <ul>${t.map(n=>`<li>${a(n)}</li>`).join("")}</ul>
    </details>
  `}function Re(e){return`
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${e.map(t=>{const n=t.file??t.filePath??t.path??"Unknown file",i=t.hops!=null?` · ${t.hops} hop${t.hops===1?"":"s"}`:"",s=t.reason?` <span style="color:var(--muted)">— ${a(t.reason)} <em>(heuristic)</em></span>`:"";return`<li style="font-family:var(--font-mono);font-size:var(--t-xs)">${a(n)}${i}${s}</li>`}).join("")}
      </ul>
    </div>
  `}function Ae(e,t){return Array.isArray(e)&&e.length>0?e.map(n=>typeof n=="string"?{file:n,hops:1}:n):(t??[]).map(n=>({file:n,hops:1}))}function Ee(e,t){return e?`${e.totalImpacted??t.length} impacted file(s) identified.`:"No dependency impact summary returned."}function T(e){return e.lineStart&&e.lineEnd?e.lineStart===e.lineEnd?String(e.lineStart):`${e.lineStart}-${e.lineEnd}`:e.lineStart?String(e.lineStart):e.line?String(e.line):""}function L(e,t){t&&x.set(e,t)}async function Q(e){const t=x.get(e);if(!t){y("Nothing to copy.",!0);return}try{if(!navigator.clipboard)throw new Error("Clipboard API is not available.");await navigator.clipboard.writeText(t),y("Copied.")}catch(n){y(`Copy failed: ${V(n)}`,!0)}}function E(e,t){b(e).addEventListener("click",()=>{t()})}function Y(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function b(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function y(e,t=!1){I.textContent=e,I.className=t?"toast show error":"toast show",window.setTimeout(()=>{I.className="toast"},2600)}function Ie(e){if(!e)return"n/a";const t=new Date(e);return Number.isNaN(t.getTime())?e:t.toLocaleString()}function V(e){return e instanceof Error?e.message:String(e)}function a(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
