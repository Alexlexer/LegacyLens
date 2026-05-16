(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const i of document.querySelectorAll('link[rel="modulepreload"]'))s(i);new MutationObserver(i=>{for(const r of i)if(r.type==="childList")for(const l of r.addedNodes)l.tagName==="LINK"&&l.rel==="modulepreload"&&s(l)}).observe(document,{childList:!0,subtree:!0});function n(i){const r={};return i.integrity&&(r.integrity=i.integrity),i.referrerPolicy&&(r.referrerPolicy=i.referrerPolicy),i.crossOrigin==="use-credentials"?r.credentials="include":i.crossOrigin==="anonymous"?r.credentials="omit":r.credentials="same-origin",r}function s(i){if(i.ep)return;i.ep=!0;const r=n(i);fetch(i.href,r)}})();const x=document.querySelector("#app");if(!x)throw new Error("App root was not found.");x.innerHTML=`
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
`;const R=F("repoPath"),O=F("useLlm"),h=w("output"),q=w("statusPill"),y=w("reports"),C=w("toast"),L=new Map;f("statusButton",H);f("previewButton",W);f("reviewButton",G);f("analyzeButton",z);f("auditButton",K);f("refreshReportsButton",v);v();async function H(){await g("Checking gpu-search...",async()=>{const e=await u("/api/search/status");h.innerHTML=E(e.isAvailable?"gpu-search is available":"gpu-search is unavailable",[`Health: ${e.health?.status??"n/a"}`,`Backend: ${e.stats?.backend??"n/a"}`,`Device: ${e.stats?.device??"n/a"}`,`Indexed files: ${e.stats?.indexedFileCount??"n/a"}`,e.error?`Error: ${e.error}`:""].filter(Boolean))})}async function W(){await g("Loading diff preview...",async()=>{const e=await u("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:R.value})});h.innerHTML=`
      ${E(`${e.changedFileCount??0} changed file(s)`,(e.files??[]).map(he))}
      <pre>${a(e.diff||"No diff content.")}</pre>
    `})}async function G(){await g("Generating review...",async()=>{const e=await u("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:R.value,useLlm:O.checked})});T(e),await v()})}async function z(){await g("Running .NET analysis...",async()=>{const e=await u("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:R.value,limitPerPreset:8})});h.innerHTML=E(`${e.findings?.length??0} preset finding(s)`,(e.findings??[]).map(t=>`${t.presetId??"preset"} · ${t.filePath??"unknown file"}${t.line?`:${t.line}`:""} · ${t.snippet??""}`))})}async function K(){await g("Running legacy audit...",async()=>{const e=document.getElementById("auditUseLlm"),t=document.getElementById("auditIncludeRoslyn"),n=document.getElementById("auditIncludeGpuSearch"),s=document.getElementById("auditIncludePresets"),i=document.getElementById("auditIncludeDi"),r=await u("/api/audit/legacy-dotnet",{method:"POST",body:JSON.stringify({repoPath:R.value,useLlm:e?.checked??!1,includeRoslyn:t?.checked??!0,includeGpuSearch:n?.checked??!0,includeDotNetPresets:s?.checked??!0,includeDependencyInjection:i?.checked??!0})});M(r),await v()})}function M(e){L.clear();const t=e.markdown??"";b("audit-markdown",t),h.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Legacy .NET Audit Report</p>
          <h2>${a(e.reportId??"Audit report")}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="audit-markdown">Copy Markdown</button>
        </div>
      </header>
      ${J(e)}
      ${Q(e)}
      ${V(e.technologySignals??[])}
      ${X(e.architectureSignals??[])}
      ${Y(e.riskFindings??[])}
      ${Z(e.roslynSummary??null)}
      ${_(e.dependencyInjectionSummary??null)}
      ${ee(e.gpuSearchSummary??null)}
      ${te(e.recommendedNextSteps??[])}
      ${e.llmSummary?o("LLM Summary",`<div class="llm-summary">${a(e.llmSummary)}</div>`):""}
      ${j(t)}
    </article>
  `,h.querySelectorAll("[data-copy]").forEach(n=>{n.addEventListener("click",()=>U(n.dataset.copy??""))})}function J(e){return o("Summary",`
      <div class="meta-grid">
        ${c("Repository",e.repoPath)}
        ${c("Generated",e.generatedAtUtc?k(e.generatedAtUtc):void 0)}
        ${c("Technology signals",String(e.technologySignals?.length??0))}
        ${c("Risk findings",String(e.riskFindings?.length??0))}
      </div>
      <p>${a(e.summary??"")}</p>
    `)}function Q(e){const t=e.workspaceSummary;return t?o("Workspace",`
      <div class="meta-grid">
        ${c("Selected workspace",t.selectedWorkspacePath??"None")}
        ${c("Kind",t.selectedWorkspaceKind??"n/a")}
        ${c("Candidates",`${t.totalCandidates??0} (${t.slnxCount??0} .slnx, ${t.slnCount??0} .sln, ${t.csprojCount??0} .csproj)`)}
      </div>
      ${t.warnings?.length?m("Warnings",t.warnings):""}
    `):""}function V(e){if(e.length===0)return o("Technology Signals",'<p class="empty">No signals detected.</p>');const t=e.map(n=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge">${a(n.category??"Signal")}</span>
        <span class="badge confidence">${a(n.confidence??"unknown")}</span>
      </div>
      <h3>${a(n.name??"Signal")}</h3>
      ${n.filePath?`<p class="mono muted">${a(n.filePath)}</p>`:""}
      <p class="muted">${a(n.evidence??"")}</p>
    </article>
  `).join("");return o("Technology Signals",`<div class="finding-list">${t}</div>`)}function X(e){if(e.length===0)return o("Architecture Signals",'<p class="empty">No signals detected.</p>');const t=e.map(n=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge confidence">${a(n.confidence??"unknown")}</span>
      </div>
      <h3>${a(n.name??"Signal")}</h3>
      <p>${a(n.message??"")}</p>
      <p class="muted">${a(n.evidence??"")}</p>
    </article>
  `).join("");return o("Architecture Signals",`<div class="finding-list">${t}</div>`)}function Y(e){if(e.length===0)return o("Risk Findings",'<p class="empty">No risk findings.</p>');const t=e.map(n=>{const s=S(n.severity);return`
      <article class="finding-card">
        <div class="row">
          <span class="badge severity-${s.toLowerCase()}">${s}</span>
          ${n.code?`<span class="muted mono">${a(n.code)}</span>`:""}
        </div>
        <h3>${a(n.title??n.code??"Finding")}</h3>
        ${n.filePath?`<p class="mono muted">${a(n.filePath)}${n.line?`:${n.line}`:""}</p>`:""}
        <p>${a(n.message??"")}</p>
        ${n.evidence?`<p class="muted"><em>Evidence: ${a(n.evidence)}</em></p>`:""}
      </article>
    `}).join("");return o("Risk Findings",`<div class="finding-list">${t}</div>`)}function Z(e){return e?e.workspaceLoaded?o("Roslyn Summary",`
      <div class="meta-grid">
        ${c("Workspace",e.workspacePath??"n/a")}
        ${c("Kind",e.workspaceKind??"n/a")}
        ${c("Projects",String(e.projectCount??0))}
        ${c("Documents",String(e.documentCount??0))}
        ${c("Symbols",String(e.symbolCount??0))}
        ${c("Classes",String(e.classCount??0))}
        ${c("Interfaces",String(e.interfaceCount??0))}
        ${c("Methods",String(e.methodCount??0))}
      </div>
      ${e.warnings?.length?m("Warnings",e.warnings):""}
    `):o("Roslyn Summary",`<p class="error">Roslyn workspace could not be loaded.</p>
       ${e.errorMessage?`<p class="muted">${a(e.errorMessage)}</p>`:""}
       ${e.warnings?.length?m("Warnings",e.warnings):""}`):o("Roslyn Summary",'<p class="empty">Roslyn analysis was not requested.</p>')}function _(e){if(!e)return o("Dependency Injection Summary",'<p class="empty">DI analysis was not requested.</p>');const t=e.registrationsByLifetime?Object.entries(e.registrationsByLifetime).map(([s,i])=>`${s}: ${i}`).join(", "):"n/a",n=(e.findings??[]).map(s=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${S(s.severity).toLowerCase()}">${S(s.severity)}</span>
        ${s.code?`<span class="muted mono">${a(s.code)}</span>`:""}
      </div>
      <p>${a(s.message??"")}</p>
      ${s.filePath?`<p class="mono muted">${a(s.filePath)}${s.line?`:${s.line}`:""}</p>`:""}
    </article>
  `).join("");return o("Dependency Injection Summary",`
      <div class="meta-grid">
        ${c("Registrations",String(e.registrationCount??0))}
        ${c("Constructor deps",String(e.constructorDependencyCount??0))}
        ${c("Findings",String(e.findingCount??0))}
        ${c("By lifetime",t)}
      </div>
      ${n?`<div class="finding-list">${n}</div>`:""}
    `)}function ee(e){if(!e)return o("gpu-search Signal Scan",'<p class="empty">gpu-search was not requested.</p>');if(!e.wasAvailable)return o("gpu-search Signal Scan",`<p class="error">gpu-search was unavailable.</p>
       ${e.errorMessage?`<p class="muted">${a(e.errorMessage)}</p>`:""}`);const t=e.usedSignalScan?'<span class="badge badge-success">Signal Scan</span>':'<span class="badge badge-warning">Fallback: Individual Queries</span>',n=e.usedSignalScan&&(e.signalCategories?.length??0)>0?`<div class="tag-list">${(e.signalCategories??[]).map(d=>`<span class="tag">${a(d)}</span>`).join("")}</div>`:"",s=(e.scanWarnings??[]).map(d=>`<p class="warning">⚠ ${a(d)}</p>`).join(""),i=(e.scanLimitations??[]).map(d=>`<li>${a(d)}</li>`).join(""),r=e.usedSignalScan?"Signals scanned":"Queries run",l=(e.results??[]).slice(0,20).map(d=>`
    <article class="context-card">
      <div class="row">
        <span class="muted mono">${a(d.query??"")}</span>
        ${d.filePath?`<span class="mono">${a(d.filePath)}${d.line?`:${d.line}`:""}</span>`:""}
      </div>
      ${d.snippet?`<pre>${a(d.snippet)}</pre>`:""}
    </article>
  `).join("");return o("gpu-search Signal Scan",`
      <div class="row">${t}</div>
      <p class="muted"><em>Results are heuristic/retrieval-based, not compiler-verified.</em></p>
      ${n}
      <div class="meta-grid">
        ${c(r,String(e.queriesRun??0))}
        ${c("Total matches",String(e.totalResults??0))}
      </div>
      ${s}
      ${i?`<ul class="muted">${i}</ul>`:""}
      ${l?`<div class="context-list">${l}</div>`:'<p class="empty">No results returned.</p>'}
    `)}function te(e){return e.length===0?o("Recommended Next Steps",'<p class="empty">No recommendations generated.</p>'):o("Recommended Next Steps",`<ul>${e.map(t=>`<li>${a(t)}</li>`).join("")}</ul>`)}async function v(){y.innerHTML='<p class="muted">Loading saved reports...</p>';try{const e=await u("/api/reports");y.innerHTML=e.length?e.map(me).join(""):'<p class="empty">No saved reports yet. Run a review or audit to create one.</p>',y.querySelectorAll("[data-view]").forEach(t=>{t.addEventListener("click",()=>ne(t.dataset.view??"",t.dataset.viewType))}),y.querySelectorAll("[data-delete]").forEach(t=>{t.addEventListener("click",()=>ae(t.dataset.delete??""))}),y.querySelectorAll("[data-export]").forEach(t=>{t.addEventListener("click",()=>se(t.dataset.export??"",t.dataset.format??""))})}catch(e){y.innerHTML=`<p class="error">Could not load saved reports: ${a(N(e))}</p>`}}async function ne(e,t){await g("Loading report...",async()=>{if(t==="LegacyAudit"){const n=await u(`/api/audit/reports/${encodeURIComponent(e)}`);M(n)}else{const n=await u(`/api/reports/${encodeURIComponent(e)}`);T(n)}})}async function ae(e){window.confirm("Delete this saved report?")&&await g("Deleting report...",async()=>{await u(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),await v(),$("Report deleted.")})}async function se(e,t){if(!e||t!=="markdown"&&t!=="html"){$("Export is unavailable for this report.",!0);return}await g(`Exporting ${t}...`,async()=>{const n=await fetch(`/api/audit/reports/${encodeURIComponent(e)}/export/${t}`);if(!n.ok){const d=await n.text();throw new Error(d||`${n.status} ${n.statusText}`)}const s=await n.blob(),i=ie(n.headers.get("content-disposition"))??`legacy-audit-${e}.${t==="markdown"?"md":"html"}`,r=URL.createObjectURL(s),l=document.createElement("a");l.href=r,l.download=i,document.body.appendChild(l),l.click(),l.remove(),URL.revokeObjectURL(r),$(`Exported ${i}.`)})}function ie(e){if(!e)return null;const t=/filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(e);return t?decodeURIComponent(t[1].replaceAll('"',"").trim()):null}function T(e){L.clear();const t=e.markdown??"",n=e.gpuSearchContext??null,s=e.roslynContext??null,i=e.llmSummary??"";b("markdown",t),n&&b("context",JSON.stringify(n,null,2)),i&&b("llm",i),h.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Review report</p>
          <h2>${a(fe(e))}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="markdown">Copy Markdown</button>
          ${n?'<button class="ghost small" data-copy="context">Copy gpu-search context</button>':""}
          ${i?'<button class="ghost small" data-copy="llm">Copy LLM summary</button>':""}
        </div>
      </header>
      ${re(e)}
      ${oe(e.findings??[])}
      ${le(n)}
      ${pe(s)}
      ${i?o("LLM Summary",`<div class="llm-summary">${a(i)}</div>`):""}
      ${j(t)}
    </article>
  `,h.querySelectorAll("[data-copy]").forEach(r=>{r.addEventListener("click",()=>U(r.dataset.copy??""))})}function re(e){const t=e.generatedAtUtc??e.createdAtUtc??e.createdAt;return o("Summary",`
      <div class="meta-grid">
        ${c("Repository",e.repoPath)}
        ${c("Created",t?k(t):void 0)}
        ${c("Changed files",String(e.changedFileCount??e.files?.length??"n/a"))}
        ${c("Provider / mode",e.providerName??e.llmProvider??e.reviewMode??e.analysisMode)}
      </div>
    `)}function oe(e){return e.length===0?o("Findings",'<p class="empty">No findings returned.</p>'):o("Findings",`<div class="finding-list">${e.map(ce).join("")}</div>`)}function ce(e){const t=S(e.severity),n=e.path??e.filePath,s=I(e);return`
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${t.toLowerCase()}">${t}</span>
        ${e.category?`<span class="badge">${a(e.category)}</span>`:""}
        ${e.ruleId?`<span class="muted mono">${a(e.ruleId)}</span>`:""}
      </div>
      <h3>${a(e.title??e.message??"Review finding")}</h3>
      ${n?`<p class="mono muted">${a(n)}${s?`:${a(s)}`:""}</p>`:""}
      ${e.description?`<p>${a(e.description)}</p>`:""}
    </article>
  `}function le(e){if(!e)return o("gpu-search Context",'<p class="empty">No gpu-search context was included in this report.</p>');if(e.wasAvailable===!1)return o("gpu-search Context",`<p class="error">gpu-search unavailable: ${a(e.unavailableReason??"No reason provided.")}</p>`);const t=e.warnings?.length?m("Global warnings",e.warnings):"",n=e.limitations?.length?m("Global limitations",e.limitations):"",s=e.files?.length?e.files.map(de).join(""):'<p class="empty">No per-file gpu-search context returned.</p>';return o("gpu-search Context",`${t}${n}<div class="context-list">${s}</div>`)}function de(e){const t=e.dependencyImpact,n=$e(t?.impactedFiles??e.impactedFiles,t?.directImporters),s=t?.warnings??e.warnings??[],i=t?.limitations??e.limitations??[],r=e.skeleton,l=typeof r=="string"?r:r?.content;return`
    <article class="context-card">
      <div class="row">
        <h3 class="mono">${a(e.filePath??e.path??"Unknown file")}</h3>
        <div class="actions compact">
          ${A(t?.confidence??e.confidence,"confidence")}
          ${A(t?.analysisMode??e.analysisMode,"mode")}
        </div>
      </div>
      ${e.error?`<p class="error">${a(e.error)}</p>`:""}
      <p class="muted">${a(t?.summary??ye(t,n))}</p>
      ${n.length?ge(n):""}
      ${s.length?m("Warnings",s):""}
      ${i.length?m("Limitations",i):""}
      ${ue(e.relatedResults??[])}
      ${l?`<details><summary>Skeleton preview${typeof r=="object"&&r?.language?` · ${a(r.language)}`:""}</summary><pre>${a(l)}</pre></details>`:""}
    </article>
  `}function ue(e){return e.length===0?'<p class="muted">No related search results.</p>':`
    <div class="related-list">
      <h4>Related search results</h4>
      ${e.map(t=>`
            <article class="related-card">
              <div class="row">
                <strong class="mono">${a(t.file??t.filePath??"Unknown file")}${I(t)?`:${a(I(t))}`:""}</strong>
                <span class="badge">${a(t.engine??"search")}${t.score!==null&&t.score!==void 0?` · ${t.score.toFixed(2)}`:""}</span>
              </div>
              ${t.reason?`<p class="muted">${a(t.reason)}</p>`:""}
              ${t.snippet?`<pre>${a(t.snippet)}</pre>`:""}
            </article>
          `).join("")}
    </div>
  `}function pe(e){if(!e)return"";if(!e.success||!e.changedSymbols?.length&&e.errorMessage)return o("Roslyn Reference Context",`<p class="error">Roslyn reference analysis was unavailable. Review continued with deterministic and gpu-search context.</p>
       ${e.errorMessage?`<p class="muted">${a(e.errorMessage)}</p>`:""}`);const t=e.changedSymbols??[];if(t.length===0)return o("Roslyn Reference Context",'<p class="empty">No changed C# symbols were matched.</p>');const n=e.symbolReferences??[],s=t.map(l=>{const d=n.filter(p=>p.symbolName===l.name&&p.symbolFullName===l.fullName&&!p.isDefinition),B=d.slice(0,10).map(p=>{const D=p.containingSymbol?` <span class="muted">— in ${a(p.containingSymbol)}</span>`:"";return`<li class="mono">${a(p.filePath??"")}:${p.line??"?"}${D}</li>`}).join("");return`
        <article class="context-card">
          <div class="row">
            <h3 class="mono">${a(l.name??"Unknown")}</h3>
            <span class="badge">${a(l.kind??"symbol")}</span>
          </div>
          <div class="meta-grid">
            ${c("Project",l.projectName)}
            ${c("Definition",l.filePath?`${l.filePath}:${l.line??"?"}`:void 0)}
            ${c("References",String(d.length))}
          </div>
          ${d.length?`<div class="mini-block"><h4>References</h4><ul>${B}</ul></div>`:""}
        </article>
      `}).join(""),i=e.warnings?.length?m("Warnings",e.warnings):"",r=e.workspacePath?`<p class="muted mono">${a(e.workspacePath)}${e.workspaceKind?` (${a(e.workspaceKind)})`:""}</p>`:"";return o("Roslyn Reference Context",`${r}${i}<div class="context-list">${s}</div>`)}function j(e){return`
    <details class="raw-markdown">
      <summary>Raw Markdown</summary>
      <pre>${a(e||"No Markdown returned.")}</pre>
    </details>
  `}async function g(e,t){P(e);try{await t(),P("Ready")}catch(n){P("Error"),$(N(n),!0)}}async function u(e,t){const n=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!n.ok){const s=await n.text();throw new Error(s||`${n.status} ${n.statusText}`)}if(n.status!==204)return await n.json()}function me(e){const t=e.reportId??e.id??"",n=e.reportType??"DiffReview",s=n==="LegacyAudit",i=s?'<span class="badge badge-success" style="font-size:0.68rem">Legacy Audit</span>':'<span class="badge" style="font-size:0.68rem">Diff Review</span>',r=s?`${k(e.generatedAtUtc??e.createdAtUtc)} · ${a(e.llmProvider??"Deterministic")}`:`${k(e.generatedAtUtc??e.createdAtUtc)} · ${e.changedFileCount??"n/a"} files · ${a(e.llmProvider??e.providerName??"n/a")}`;return`
    <article class="report">
      <div>
        <strong>${a(e.repoPath??"Unknown repository")}</strong>
        <span>${i} ${r}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${a(t)}" data-view-type="${a(n)}">View</button>
        ${s?`<button class="ghost small" data-export="${a(t)}" data-format="markdown">Export Markdown</button>`:""}
        ${s?`<button class="ghost small" data-export="${a(t)}" data-format="html">Export HTML</button>`:""}
        <button class="danger small" data-delete="${a(t)}">Delete</button>
      </div>
    </article>
  `}function E(e,t){return`
    <div class="result-card">
      <h3>${a(e)}</h3>
      <ul>${t.map(n=>`<li>${a(n)}</li>`).join("")}</ul>
    </div>
  `}function o(e,t){return`
    <section class="viewer-section">
      <h2>${a(e)}</h2>
      ${t}
    </section>
  `}function c(e,t){return`
    <div class="meta-item">
      <span>${a(e)}</span>
      <strong>${a(t||"n/a")}</strong>
    </div>
  `}function ge(e){return`
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${e.map(t=>{const n=t.file??t.filePath??t.path??"Unknown file",s=t.hops!==null&&t.hops!==void 0?` · ${t.hops} hop${t.hops===1?"":"s"}`:"",i=t.reason?` <span class="reason">— ${a(t.reason)} <em>(heuristic)</em></span>`:"";return`<li class="mono">${a(n)}${s}${i}</li>`}).join("")}
      </ul>
    </div>
  `}function $e(e,t){return Array.isArray(e)&&e.length>0?e.map(n=>typeof n=="string"?{file:n,hops:1}:n):(t??[]).map(n=>({file:n,hops:1}))}function m(e,t){return`
    <details>
      <summary>${a(e)} (${t.length})</summary>
      <ul>${t.map(n=>`<li>${a(n)}</li>`).join("")}</ul>
    </details>
  `}function A(e,t=""){return e?`<span class="badge ${t}">${a(e)}</span>`:""}function he(e){return`${e.status??"M"} ${e.path??e.filePath??"Unknown file"} (+${e.additions??0}/-${e.deletions??0})`}function ye(e,t){return e?`${e.totalImpacted??t.length} impacted file(s) identified.`:"No dependency impact summary returned."}function fe(e){return e.reportId??e.id??"Review report"}function I(e){return e.lineStart&&e.lineEnd?e.lineStart===e.lineEnd?String(e.lineStart):`${e.lineStart}-${e.lineEnd}`:e.lineStart?String(e.lineStart):e.line?String(e.line):""}function S(e){const t=(e??"Info").toLowerCase();return["low","medium","high","critical"].includes(t)?t[0].toUpperCase()+t.slice(1):"Info"}function b(e,t){t&&L.set(e,t)}async function U(e){const t=L.get(e);if(!t){$("Nothing to copy.",!0);return}try{if(!navigator.clipboard)throw new Error("Clipboard API is not available.");await navigator.clipboard.writeText(t),$("Copied.")}catch(n){$(`Copy failed: ${N(n)}`,!0)}}function f(e,t){w(e).addEventListener("click",()=>{t()})}function F(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function w(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function P(e){q.textContent=e}function $(e,t=!1){C.textContent=e,C.className=t?"toast show error":"toast show",window.setTimeout(()=>{C.className="toast"},2600)}function k(e){if(!e)return"n/a";const t=new Date(e);return Number.isNaN(t.getTime())?e:t.toLocaleString()}function N(e){return e instanceof Error?e.message:String(e)}function a(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
