(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const i of document.querySelectorAll('link[rel="modulepreload"]'))s(i);new MutationObserver(i=>{for(const o of i)if(o.type==="childList")for(const c of o.addedNodes)c.tagName==="LINK"&&c.rel==="modulepreload"&&s(c)}).observe(document,{childList:!0,subtree:!0});function n(i){const o={};return i.integrity&&(o.integrity=i.integrity),i.referrerPolicy&&(o.referrerPolicy=i.referrerPolicy),i.crossOrigin==="use-credentials"?o.credentials="include":i.crossOrigin==="anonymous"?o.credentials="omit":o.credentials="same-origin",o}function s(i){if(i.ep)return;i.ep=!0;const o=n(i);fetch(i.href,o)}})();const j=document.querySelector("#app");if(!j)throw new Error("App root was not found.");j.innerHTML=`
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
        <details class="audit-options">
          <summary>Ollama model</summary>
          <div class="stack audit-form">
            <div id="ollamaStatus" class="muted">Status not checked.</div>
            <div class="actions compact">
              <button id="ollamaStatusButton" class="ghost small">Refresh Ollama status</button>
              <button id="ollamaPullButton" class="secondary small" disabled>Pull configured model</button>
            </div>
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
`;const P=H("repoPath"),G=H("useLlm"),y=v("output"),z=v("statusPill"),f=v("reports"),I=v("toast"),K=v("ollamaStatus"),C=new Map;let b=null,w=!1;h("statusButton",J);h("previewButton",V);h("reviewButton",X);h("analyzeButton",Z);h("auditButton",_);h("refreshReportsButton",S);h("ollamaStatusButton",Q);h("ollamaPullButton",Y);S();async function J(){await p("Checking gpu-search...",async()=>{const e=await u("/api/search/status");y.innerHTML=M(e.isAvailable?"gpu-search is available":"gpu-search is unavailable",[`Health: ${e.health?.status??"n/a"}`,`Backend: ${e.stats?.backend??"n/a"}`,`Device: ${e.stats?.device??"n/a"}`,`Indexed files: ${e.stats?.indexedFileCount??"n/a"}`,e.error?`Error: ${e.error}`:""].filter(Boolean))})}async function Q(){await p("Checking Ollama...",async()=>{b=await u("/api/llm/ollama/status"),B(b)})}async function Y(){w||(w=!0,E(),await p("Pulling Ollama model...",async()=>{const e=await u("/api/llm/ollama/pull",{method:"POST",body:JSON.stringify({})});if(!e.success)throw new Error(e.error?`${e.message} ${e.error}`:e.message??"Ollama pull failed.");g(e.message??"Ollama model pulled."),b=await u("/api/llm/ollama/status"),B(b)}),w=!1,E())}function B(e){const t=e.installedModels??[],n=t.length<=5?t.join(", ")||"none":`${t.length} installed model(s)`;K.innerHTML=`
    <div class="meta-grid">
      ${r("Reachable",e.serverReachable?"Yes":"No")}
      ${r("Configured model",e.configuredModel)}
      ${r("Model installed",e.modelInstalled?"Yes":"No")}
      ${r("Installed",n)}
    </div>
    <p class="${e.serverReachable&&e.modelInstalled?"muted":"error"}">${a(e.message??"")}</p>
    ${e.error?`<p class="muted">${a(e.error)}</p>`:""}
  `,E()}function E(){const e=document.getElementById("ollamaPullButton");if(!e)return;const t=!!b?.serverReachable;e.disabled=!t||w,e.textContent=w?"Pulling...":"Pull configured model"}async function V(){await p("Loading diff preview...",async()=>{const e=await u("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:P.value})});y.innerHTML=`
      ${M(`${e.changedFileCount??0} changed file(s)`,(e.files??[]).map(ke))}
      <pre>${a(e.diff||"No diff content.")}</pre>
    `})}async function X(){await p("Generating review...",async()=>{const e=await u("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:P.value,useLlm:G.checked})});U(e),await S()})}async function Z(){await p("Running .NET analysis...",async()=>{const e=await u("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:P.value,limitPerPreset:8})});y.innerHTML=M(`${e.findings?.length??0} preset finding(s)`,(e.findings??[]).map(t=>`${t.presetId??"preset"} · ${t.filePath??"unknown file"}${t.line?`:${t.line}`:""} · ${t.snippet??""}`))})}async function _(){await p("Running legacy audit...",async()=>{const e=document.getElementById("auditUseLlm"),t=document.getElementById("auditIncludeRoslyn"),n=document.getElementById("auditIncludeGpuSearch"),s=document.getElementById("auditIncludePresets"),i=document.getElementById("auditIncludeDi"),o=await u("/api/audit/legacy-dotnet",{method:"POST",body:JSON.stringify({repoPath:P.value,useLlm:e?.checked??!1,includeRoslyn:t?.checked??!0,includeGpuSearch:n?.checked??!0,includeDotNetPresets:s?.checked??!0,includeDependencyInjection:i?.checked??!0})});O(o),await S()})}function O(e){C.clear();const t=e.markdown??"";k("audit-markdown",t),y.innerHTML=`
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
      ${ee(e)}
      ${te(e)}
      ${ne(e.technologySignals??[])}
      ${ae(e.architectureSignals??[])}
      ${se(e.riskFindings??[])}
      ${ie(e.roslynSummary??null)}
      ${oe(e.dependencyInjectionSummary??null)}
      ${re(e.gpuSearchSummary??null)}
      ${le(e.recommendedNextSteps??[])}
      ${e.llmSummary?l("LLM Summary",`<div class="llm-summary">${a(e.llmSummary)}</div>`):""}
      ${F(t)}
    </article>
  `,y.querySelectorAll("[data-copy]").forEach(n=>{n.addEventListener("click",()=>D(n.dataset.copy??""))})}function ee(e){return l("Summary",`
      <div class="meta-grid">
        ${r("Repository",e.repoPath)}
        ${r("Generated",e.generatedAtUtc?L(e.generatedAtUtc):void 0)}
        ${r("Technology signals",String(e.technologySignals?.length??0))}
        ${r("Risk findings",String(e.riskFindings?.length??0))}
      </div>
      <p>${a(e.summary??"")}</p>
    `)}function te(e){const t=e.workspaceSummary;return t?l("Workspace",`
      <div class="meta-grid">
        ${r("Selected workspace",t.selectedWorkspacePath??"None")}
        ${r("Kind",t.selectedWorkspaceKind??"n/a")}
        ${r("Candidates",`${t.totalCandidates??0} (${t.slnxCount??0} .slnx, ${t.slnCount??0} .sln, ${t.csprojCount??0} .csproj)`)}
      </div>
      ${t.warnings?.length?$("Warnings",t.warnings):""}
    `):""}function ne(e){if(e.length===0)return l("Technology Signals",'<p class="empty">No signals detected.</p>');const t=e.map(n=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge">${a(n.category??"Signal")}</span>
        <span class="badge confidence">${a(n.confidence??"unknown")}</span>
      </div>
      <h3>${a(n.name??"Signal")}</h3>
      ${n.filePath?`<p class="mono muted">${a(n.filePath)}</p>`:""}
      <p class="muted">${a(n.evidence??"")}</p>
    </article>
  `).join("");return l("Technology Signals",`<div class="finding-list">${t}</div>`)}function ae(e){if(e.length===0)return l("Architecture Signals",'<p class="empty">No signals detected.</p>');const t=e.map(n=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge confidence">${a(n.confidence??"unknown")}</span>
      </div>
      <h3>${a(n.name??"Signal")}</h3>
      <p>${a(n.message??"")}</p>
      <p class="muted">${a(n.evidence??"")}</p>
    </article>
  `).join("");return l("Architecture Signals",`<div class="finding-list">${t}</div>`)}function se(e){if(e.length===0)return l("Risk Findings",'<p class="empty">No risk findings.</p>');const t=e.map(n=>{const s=R(n.severity);return`
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
    `}).join("");return l("Risk Findings",`<div class="finding-list">${t}</div>`)}function ie(e){return e?e.workspaceLoaded?l("Roslyn Summary",`
      <div class="meta-grid">
        ${r("Workspace",e.workspacePath??"n/a")}
        ${r("Kind",e.workspaceKind??"n/a")}
        ${r("Projects",String(e.projectCount??0))}
        ${r("Documents",String(e.documentCount??0))}
        ${r("Symbols",String(e.symbolCount??0))}
        ${r("Classes",String(e.classCount??0))}
        ${r("Interfaces",String(e.interfaceCount??0))}
        ${r("Methods",String(e.methodCount??0))}
      </div>
      ${e.warnings?.length?$("Warnings",e.warnings):""}
    `):l("Roslyn Summary",`<p class="error">Roslyn workspace could not be loaded.</p>
       ${e.errorMessage?`<p class="muted">${a(e.errorMessage)}</p>`:""}
       ${e.warnings?.length?$("Warnings",e.warnings):""}`):l("Roslyn Summary",'<p class="empty">Roslyn analysis was not requested.</p>')}function oe(e){if(!e)return l("Dependency Injection Summary",'<p class="empty">DI analysis was not requested.</p>');const t=e.registrationsByLifetime?Object.entries(e.registrationsByLifetime).map(([s,i])=>`${s}: ${i}`).join(", "):"n/a",n=(e.findings??[]).map(s=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${R(s.severity).toLowerCase()}">${R(s.severity)}</span>
        ${s.code?`<span class="muted mono">${a(s.code)}</span>`:""}
      </div>
      <p>${a(s.message??"")}</p>
      ${s.filePath?`<p class="mono muted">${a(s.filePath)}${s.line?`:${s.line}`:""}</p>`:""}
    </article>
  `).join("");return l("Dependency Injection Summary",`
      <div class="meta-grid">
        ${r("Registrations",String(e.registrationCount??0))}
        ${r("Constructor deps",String(e.constructorDependencyCount??0))}
        ${r("Findings",String(e.findingCount??0))}
        ${r("By lifetime",t)}
      </div>
      ${n?`<div class="finding-list">${n}</div>`:""}
    `)}function re(e){if(!e)return l("gpu-search Signal Scan",'<p class="empty">gpu-search was not requested.</p>');if(!e.wasAvailable)return l("gpu-search Signal Scan",`<p class="error">gpu-search was unavailable.</p>
       ${e.errorMessage?`<p class="muted">${a(e.errorMessage)}</p>`:""}`);const t=e.usedSignalScan?'<span class="badge badge-success">Signal Scan</span>':'<span class="badge badge-warning">Fallback: Individual Queries</span>',n=e.usedSignalScan&&(e.signalCategories?.length??0)>0?`<div class="tag-list">${(e.signalCategories??[]).map(d=>`<span class="tag">${a(d)}</span>`).join("")}</div>`:"",s=(e.scanWarnings??[]).map(d=>`<p class="warning">⚠ ${a(d)}</p>`).join(""),i=(e.scanLimitations??[]).map(d=>`<li>${a(d)}</li>`).join(""),o=e.usedSignalScan?"Signals scanned":"Queries run",c=(e.results??[]).slice(0,20).map(d=>`
    <article class="context-card">
      <div class="row">
        <span class="muted mono">${a(d.query??"")}</span>
        ${d.filePath?`<span class="mono">${a(d.filePath)}${d.line?`:${d.line}`:""}</span>`:""}
      </div>
      ${d.snippet?`<pre>${a(d.snippet)}</pre>`:""}
    </article>
  `).join("");return l("gpu-search Signal Scan",`
      <div class="row">${t}</div>
      <p class="muted"><em>Results are heuristic/retrieval-based, not compiler-verified.</em></p>
      ${n}
      <div class="meta-grid">
        ${r(o,String(e.queriesRun??0))}
        ${r("Total matches",String(e.totalResults??0))}
      </div>
      ${s}
      ${i?`<ul class="muted">${i}</ul>`:""}
      ${c?`<div class="context-list">${c}</div>`:'<p class="empty">No results returned.</p>'}
    `)}function le(e){return e.length===0?l("Recommended Next Steps",'<p class="empty">No recommendations generated.</p>'):l("Recommended Next Steps",`<ul>${e.map(t=>`<li>${a(t)}</li>`).join("")}</ul>`)}async function S(){f.innerHTML='<p class="muted">Loading saved reports...</p>';try{const e=await u("/api/reports");f.innerHTML=e.length?e.map(we).join(""):'<p class="empty">No saved reports yet. Run a review or audit to create one.</p>',f.querySelectorAll("[data-view]").forEach(t=>{t.addEventListener("click",()=>ce(t.dataset.view??"",t.dataset.viewType))}),f.querySelectorAll("[data-delete]").forEach(t=>{t.addEventListener("click",()=>de(t.dataset.delete??""))}),f.querySelectorAll("[data-export]").forEach(t=>{t.addEventListener("click",()=>ue(t.dataset.export??"",t.dataset.format??""))})}catch(e){f.innerHTML=`<p class="error">Could not load saved reports: ${a(x(e))}</p>`}}async function ce(e,t){await p("Loading report...",async()=>{if(t==="LegacyAudit"){const n=await u(`/api/audit/reports/${encodeURIComponent(e)}`);O(n)}else{const n=await u(`/api/reports/${encodeURIComponent(e)}`);U(n)}})}async function de(e){window.confirm("Delete this saved report?")&&await p("Deleting report...",async()=>{await u(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),await S(),g("Report deleted.")})}async function ue(e,t){if(!e||t!=="markdown"&&t!=="html"){g("Export is unavailable for this report.",!0);return}await p(`Exporting ${t}...`,async()=>{const n=await fetch(`/api/audit/reports/${encodeURIComponent(e)}/export/${t}`);if(!n.ok){const d=await n.text();throw new Error(d||`${n.status} ${n.statusText}`)}const s=await n.blob(),i=pe(n.headers.get("content-disposition"))??`legacy-audit-${e}.${t==="markdown"?"md":"html"}`,o=URL.createObjectURL(s),c=document.createElement("a");c.href=o,c.download=i,document.body.appendChild(c),c.click(),c.remove(),URL.revokeObjectURL(o),g(`Exported ${i}.`)})}function pe(e){if(!e)return null;const t=/filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(e);return t?decodeURIComponent(t[1].replaceAll('"',"").trim()):null}function U(e){C.clear();const t=e.markdown??"",n=e.gpuSearchContext??null,s=e.roslynContext??null,i=e.llmSummary??"";k("markdown",t),n&&k("context",JSON.stringify(n,null,2)),i&&k("llm",i),y.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Review report</p>
          <h2>${a(Le(e))}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="markdown">Copy Markdown</button>
          ${n?'<button class="ghost small" data-copy="context">Copy gpu-search context</button>':""}
          ${i?'<button class="ghost small" data-copy="llm">Copy LLM summary</button>':""}
        </div>
      </header>
      ${me(e)}
      ${ge(e.findings??[])}
      ${he(n)}
      ${ve(s)}
      ${i?l("LLM Summary",`<div class="llm-summary">${a(i)}</div>`):""}
      ${F(t)}
    </article>
  `,y.querySelectorAll("[data-copy]").forEach(o=>{o.addEventListener("click",()=>D(o.dataset.copy??""))})}function me(e){const t=e.generatedAtUtc??e.createdAtUtc??e.createdAt;return l("Summary",`
      <div class="meta-grid">
        ${r("Repository",e.repoPath)}
        ${r("Created",t?L(t):void 0)}
        ${r("Changed files",String(e.changedFileCount??e.files?.length??"n/a"))}
        ${r("Provider / mode",e.providerName??e.llmProvider??e.reviewMode??e.analysisMode)}
      </div>
    `)}function ge(e){return e.length===0?l("Findings",'<p class="empty">No findings returned.</p>'):l("Findings",`<div class="finding-list">${e.map($e).join("")}</div>`)}function $e(e){const t=R(e.severity),n=e.path??e.filePath,s=A(e);return`
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
  `}function he(e){if(!e)return l("gpu-search Context",'<p class="empty">No gpu-search context was included in this report.</p>');if(e.wasAvailable===!1)return l("gpu-search Context",`<p class="error">gpu-search unavailable: ${a(e.unavailableReason??"No reason provided.")}</p>`);const t=e.warnings?.length?$("Global warnings",e.warnings):"",n=e.limitations?.length?$("Global limitations",e.limitations):"",s=e.files?.length?e.files.map(ye).join(""):'<p class="empty">No per-file gpu-search context returned.</p>';return l("gpu-search Context",`${t}${n}<div class="context-list">${s}</div>`)}function ye(e){const t=e.dependencyImpact,n=Se(t?.impactedFiles??e.impactedFiles,t?.directImporters),s=t?.warnings??e.warnings??[],i=t?.limitations??e.limitations??[],o=e.skeleton,c=typeof o=="string"?o:o?.content;return`
    <article class="context-card">
      <div class="row">
        <h3 class="mono">${a(e.filePath??e.path??"Unknown file")}</h3>
        <div class="actions compact">
          ${T(t?.confidence??e.confidence,"confidence")}
          ${T(t?.analysisMode??e.analysisMode,"mode")}
        </div>
      </div>
      ${e.error?`<p class="error">${a(e.error)}</p>`:""}
      <p class="muted">${a(t?.summary??Re(t,n))}</p>
      ${n.length?be(n):""}
      ${s.length?$("Warnings",s):""}
      ${i.length?$("Limitations",i):""}
      ${fe(e.relatedResults??[])}
      ${c?`<details><summary>Skeleton preview${typeof o=="object"&&o?.language?` · ${a(o.language)}`:""}</summary><pre>${a(c)}</pre></details>`:""}
    </article>
  `}function fe(e){return e.length===0?'<p class="muted">No related search results.</p>':`
    <div class="related-list">
      <h4>Related search results</h4>
      ${e.map(t=>`
            <article class="related-card">
              <div class="row">
                <strong class="mono">${a(t.file??t.filePath??"Unknown file")}${A(t)?`:${a(A(t))}`:""}</strong>
                <span class="badge">${a(t.engine??"search")}${t.score!==null&&t.score!==void 0?` · ${t.score.toFixed(2)}`:""}</span>
              </div>
              ${t.reason?`<p class="muted">${a(t.reason)}</p>`:""}
              ${t.snippet?`<pre>${a(t.snippet)}</pre>`:""}
            </article>
          `).join("")}
    </div>
  `}function ve(e){if(!e)return"";if(!e.success||!e.changedSymbols?.length&&e.errorMessage)return l("Roslyn Reference Context",`<p class="error">Roslyn reference analysis was unavailable. Review continued with deterministic and gpu-search context.</p>
       ${e.errorMessage?`<p class="muted">${a(e.errorMessage)}</p>`:""}`);const t=e.changedSymbols??[];if(t.length===0)return l("Roslyn Reference Context",'<p class="empty">No changed C# symbols were matched.</p>');const n=e.symbolReferences??[],s=t.map(c=>{const d=n.filter(m=>m.symbolName===c.name&&m.symbolFullName===c.fullName&&!m.isDefinition),q=d.slice(0,10).map(m=>{const W=m.containingSymbol?` <span class="muted">— in ${a(m.containingSymbol)}</span>`:"";return`<li class="mono">${a(m.filePath??"")}:${m.line??"?"}${W}</li>`}).join("");return`
        <article class="context-card">
          <div class="row">
            <h3 class="mono">${a(c.name??"Unknown")}</h3>
            <span class="badge">${a(c.kind??"symbol")}</span>
          </div>
          <div class="meta-grid">
            ${r("Project",c.projectName)}
            ${r("Definition",c.filePath?`${c.filePath}:${c.line??"?"}`:void 0)}
            ${r("References",String(d.length))}
          </div>
          ${d.length?`<div class="mini-block"><h4>References</h4><ul>${q}</ul></div>`:""}
        </article>
      `}).join(""),i=e.warnings?.length?$("Warnings",e.warnings):"",o=e.workspacePath?`<p class="muted mono">${a(e.workspacePath)}${e.workspaceKind?` (${a(e.workspaceKind)})`:""}</p>`:"";return l("Roslyn Reference Context",`${o}${i}<div class="context-list">${s}</div>`)}function F(e){return`
    <details class="raw-markdown">
      <summary>Raw Markdown</summary>
      <pre>${a(e||"No Markdown returned.")}</pre>
    </details>
  `}async function p(e,t){N(e);try{await t(),N("Ready")}catch(n){N("Error"),g(x(n),!0)}}async function u(e,t){const n=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!n.ok){const s=await n.text();throw new Error(s||`${n.status} ${n.statusText}`)}if(n.status!==204)return await n.json()}function we(e){const t=e.reportId??e.id??"",n=e.reportType??"DiffReview",s=n==="LegacyAudit",i=s?'<span class="badge badge-success" style="font-size:0.68rem">Legacy Audit</span>':'<span class="badge" style="font-size:0.68rem">Diff Review</span>',o=s?`${L(e.generatedAtUtc??e.createdAtUtc)} · ${a(e.llmProvider??"Deterministic")}`:`${L(e.generatedAtUtc??e.createdAtUtc)} · ${e.changedFileCount??"n/a"} files · ${a(e.llmProvider??e.providerName??"n/a")}`;return`
    <article class="report">
      <div>
        <strong>${a(e.repoPath??"Unknown repository")}</strong>
        <span>${i} ${o}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${a(t)}" data-view-type="${a(n)}">View</button>
        ${s?`<button class="ghost small" data-export="${a(t)}" data-format="markdown">Export Markdown</button>`:""}
        ${s?`<button class="ghost small" data-export="${a(t)}" data-format="html">Export HTML</button>`:""}
        <button class="danger small" data-delete="${a(t)}">Delete</button>
      </div>
    </article>
  `}function M(e,t){return`
    <div class="result-card">
      <h3>${a(e)}</h3>
      <ul>${t.map(n=>`<li>${a(n)}</li>`).join("")}</ul>
    </div>
  `}function l(e,t){return`
    <section class="viewer-section">
      <h2>${a(e)}</h2>
      ${t}
    </section>
  `}function r(e,t){return`
    <div class="meta-item">
      <span>${a(e)}</span>
      <strong>${a(t||"n/a")}</strong>
    </div>
  `}function be(e){return`
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${e.map(t=>{const n=t.file??t.filePath??t.path??"Unknown file",s=t.hops!==null&&t.hops!==void 0?` · ${t.hops} hop${t.hops===1?"":"s"}`:"",i=t.reason?` <span class="reason">— ${a(t.reason)} <em>(heuristic)</em></span>`:"";return`<li class="mono">${a(n)}${s}${i}</li>`}).join("")}
      </ul>
    </div>
  `}function Se(e,t){return Array.isArray(e)&&e.length>0?e.map(n=>typeof n=="string"?{file:n,hops:1}:n):(t??[]).map(n=>({file:n,hops:1}))}function $(e,t){return`
    <details>
      <summary>${a(e)} (${t.length})</summary>
      <ul>${t.map(n=>`<li>${a(n)}</li>`).join("")}</ul>
    </details>
  `}function T(e,t=""){return e?`<span class="badge ${t}">${a(e)}</span>`:""}function ke(e){return`${e.status??"M"} ${e.path??e.filePath??"Unknown file"} (+${e.additions??0}/-${e.deletions??0})`}function Re(e,t){return e?`${e.totalImpacted??t.length} impacted file(s) identified.`:"No dependency impact summary returned."}function Le(e){return e.reportId??e.id??"Review report"}function A(e){return e.lineStart&&e.lineEnd?e.lineStart===e.lineEnd?String(e.lineStart):`${e.lineStart}-${e.lineEnd}`:e.lineStart?String(e.lineStart):e.line?String(e.line):""}function R(e){const t=(e??"Info").toLowerCase();return["low","medium","high","critical"].includes(t)?t[0].toUpperCase()+t.slice(1):"Info"}function k(e,t){t&&C.set(e,t)}async function D(e){const t=C.get(e);if(!t){g("Nothing to copy.",!0);return}try{if(!navigator.clipboard)throw new Error("Clipboard API is not available.");await navigator.clipboard.writeText(t),g("Copied.")}catch(n){g(`Copy failed: ${x(n)}`,!0)}}function h(e,t){v(e).addEventListener("click",()=>{t()})}function H(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function v(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function N(e){z.textContent=e}function g(e,t=!1){I.textContent=e,I.className=t?"toast show error":"toast show",window.setTimeout(()=>{I.className="toast"},2600)}function L(e){if(!e)return"n/a";const t=new Date(e);return Number.isNaN(t.getTime())?e:t.toLocaleString()}function x(e){return e instanceof Error?e.message:String(e)}function a(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
