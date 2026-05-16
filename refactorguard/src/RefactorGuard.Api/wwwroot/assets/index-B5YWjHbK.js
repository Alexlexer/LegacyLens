(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const i of document.querySelectorAll('link[rel="modulepreload"]'))a(i);new MutationObserver(i=>{for(const c of i)if(c.type==="childList")for(const l of c.addedNodes)l.tagName==="LINK"&&l.rel==="modulepreload"&&a(l)}).observe(document,{childList:!0,subtree:!0});function n(i){const c={};return i.integrity&&(c.integrity=i.integrity),i.referrerPolicy&&(c.referrerPolicy=i.referrerPolicy),i.crossOrigin==="use-credentials"?c.credentials="include":i.crossOrigin==="anonymous"?c.credentials="omit":c.credentials="same-origin",c}function a(i){if(i.ep)return;i.ep=!0;const c=n(i);fetch(i.href,c)}})();const M=document.querySelector("#app");if(!M)throw new Error("App root was not found.");M.innerHTML=`
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
`;const S=x("repoPath"),U=x("useLlm"),m=f("output"),O=f("statusPill"),$=f("reports"),L=f("toast"),b=new Map;h("statusButton",q);h("previewButton",H);h("reviewButton",G);h("analyzeButton",W);h("auditButton",K);h("refreshReportsButton",k);k();async function q(){await g("Checking gpu-search...",async()=>{const e=await p("/api/search/status");m.innerHTML=I(e.isAvailable?"gpu-search is available":"gpu-search is unavailable",[`Health: ${e.health?.status??"n/a"}`,`Backend: ${e.stats?.backend??"n/a"}`,`Device: ${e.stats?.device??"n/a"}`,`Indexed files: ${e.stats?.indexedFileCount??"n/a"}`,e.error?`Error: ${e.error}`:""].filter(Boolean))})}async function H(){await g("Loading diff preview...",async()=>{const e=await p("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:S.value})});m.innerHTML=`
      ${I(`${e.changedFileCount??0} changed file(s)`,(e.files??[]).map(ge))}
      <pre>${s(e.diff||"No diff content.")}</pre>
    `})}async function G(){await g("Generating review...",async()=>{const e=await p("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:S.value,useLlm:U.checked})});T(e),await k()})}async function W(){await g("Running .NET analysis...",async()=>{const e=await p("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:S.value,limitPerPreset:8})});m.innerHTML=I(`${e.findings?.length??0} preset finding(s)`,(e.findings??[]).map(t=>`${t.presetId??"preset"} · ${t.filePath??"unknown file"}${t.line?`:${t.line}`:""} · ${t.snippet??""}`))})}async function K(){await g("Running legacy audit...",async()=>{const e=document.getElementById("auditUseLlm"),t=document.getElementById("auditIncludeRoslyn"),n=document.getElementById("auditIncludeGpuSearch"),a=document.getElementById("auditIncludePresets"),i=document.getElementById("auditIncludeDi"),c=await p("/api/audit/legacy-dotnet",{method:"POST",body:JSON.stringify({repoPath:S.value,useLlm:e?.checked??!1,includeRoslyn:t?.checked??!0,includeGpuSearch:n?.checked??!0,includeDotNetPresets:a?.checked??!0,includeDependencyInjection:i?.checked??!0})});z(c)})}function z(e){b.clear();const t=e.markdown??"";v("audit-markdown",t),m.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Legacy .NET Audit Report</p>
          <h2>${s(e.reportId??"Audit report")}</h2>
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
      ${e.llmSummary?r("LLM Summary",`<div class="llm-summary">${s(e.llmSummary)}</div>`):""}
      ${j(t)}
    </article>
  `,m.querySelectorAll("[data-copy]").forEach(n=>{n.addEventListener("click",()=>F(n.dataset.copy??""))})}function J(e){return r("Summary",`
      <div class="meta-grid">
        ${o("Repository",e.repoPath)}
        ${o("Generated",e.generatedAtUtc?N(e.generatedAtUtc):void 0)}
        ${o("Technology signals",String(e.technologySignals?.length??0))}
        ${o("Risk findings",String(e.riskFindings?.length??0))}
      </div>
      <p>${s(e.summary??"")}</p>
    `)}function Q(e){const t=e.workspaceSummary;return t?r("Workspace",`
      <div class="meta-grid">
        ${o("Selected workspace",t.selectedWorkspacePath??"None")}
        ${o("Kind",t.selectedWorkspaceKind??"n/a")}
        ${o("Candidates",`${t.totalCandidates??0} (${t.slnxCount??0} .slnx, ${t.slnCount??0} .sln, ${t.csprojCount??0} .csproj)`)}
      </div>
      ${t.warnings?.length?u("Warnings",t.warnings):""}
    `):""}function V(e){if(e.length===0)return r("Technology Signals",'<p class="empty">No signals detected.</p>');const t=e.map(n=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge">${s(n.category??"Signal")}</span>
        <span class="badge confidence">${s(n.confidence??"unknown")}</span>
      </div>
      <h3>${s(n.name??"Signal")}</h3>
      ${n.filePath?`<p class="mono muted">${s(n.filePath)}</p>`:""}
      <p class="muted">${s(n.evidence??"")}</p>
    </article>
  `).join("");return r("Technology Signals",`<div class="finding-list">${t}</div>`)}function X(e){if(e.length===0)return r("Architecture Signals",'<p class="empty">No signals detected.</p>');const t=e.map(n=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge confidence">${s(n.confidence??"unknown")}</span>
      </div>
      <h3>${s(n.name??"Signal")}</h3>
      <p>${s(n.message??"")}</p>
      <p class="muted">${s(n.evidence??"")}</p>
    </article>
  `).join("");return r("Architecture Signals",`<div class="finding-list">${t}</div>`)}function Y(e){if(e.length===0)return r("Risk Findings",'<p class="empty">No risk findings.</p>');const t=e.map(n=>{const a=w(n.severity);return`
      <article class="finding-card">
        <div class="row">
          <span class="badge severity-${a.toLowerCase()}">${a}</span>
          ${n.code?`<span class="muted mono">${s(n.code)}</span>`:""}
        </div>
        <h3>${s(n.title??n.code??"Finding")}</h3>
        ${n.filePath?`<p class="mono muted">${s(n.filePath)}${n.line?`:${n.line}`:""}</p>`:""}
        <p>${s(n.message??"")}</p>
        ${n.evidence?`<p class="muted"><em>Evidence: ${s(n.evidence)}</em></p>`:""}
      </article>
    `}).join("");return r("Risk Findings",`<div class="finding-list">${t}</div>`)}function Z(e){return e?e.workspaceLoaded?r("Roslyn Summary",`
      <div class="meta-grid">
        ${o("Workspace",e.workspacePath??"n/a")}
        ${o("Kind",e.workspaceKind??"n/a")}
        ${o("Projects",String(e.projectCount??0))}
        ${o("Documents",String(e.documentCount??0))}
        ${o("Symbols",String(e.symbolCount??0))}
        ${o("Classes",String(e.classCount??0))}
        ${o("Interfaces",String(e.interfaceCount??0))}
        ${o("Methods",String(e.methodCount??0))}
      </div>
      ${e.warnings?.length?u("Warnings",e.warnings):""}
    `):r("Roslyn Summary",`<p class="error">Roslyn workspace could not be loaded.</p>
       ${e.errorMessage?`<p class="muted">${s(e.errorMessage)}</p>`:""}
       ${e.warnings?.length?u("Warnings",e.warnings):""}`):r("Roslyn Summary",'<p class="empty">Roslyn analysis was not requested.</p>')}function _(e){if(!e)return r("Dependency Injection Summary",'<p class="empty">DI analysis was not requested.</p>');const t=e.registrationsByLifetime?Object.entries(e.registrationsByLifetime).map(([a,i])=>`${a}: ${i}`).join(", "):"n/a",n=(e.findings??[]).map(a=>`
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${w(a.severity).toLowerCase()}">${w(a.severity)}</span>
        ${a.code?`<span class="muted mono">${s(a.code)}</span>`:""}
      </div>
      <p>${s(a.message??"")}</p>
      ${a.filePath?`<p class="mono muted">${s(a.filePath)}${a.line?`:${a.line}`:""}</p>`:""}
    </article>
  `).join("");return r("Dependency Injection Summary",`
      <div class="meta-grid">
        ${o("Registrations",String(e.registrationCount??0))}
        ${o("Constructor deps",String(e.constructorDependencyCount??0))}
        ${o("Findings",String(e.findingCount??0))}
        ${o("By lifetime",t)}
      </div>
      ${n?`<div class="finding-list">${n}</div>`:""}
    `)}function ee(e){if(!e)return r("gpu-search Findings",'<p class="empty">gpu-search was not requested.</p>');if(!e.wasAvailable)return r("gpu-search Findings",`<p class="error">gpu-search was unavailable.</p>
       ${e.errorMessage?`<p class="muted">${s(e.errorMessage)}</p>`:""}`);const t=(e.results??[]).slice(0,20).map(n=>`
    <article class="context-card">
      <div class="row">
        <span class="muted mono">${s(n.query??"")}</span>
        ${n.filePath?`<span class="mono">${s(n.filePath)}${n.line?`:${n.line}`:""}</span>`:""}
      </div>
      ${n.snippet?`<pre>${s(n.snippet)}</pre>`:""}
    </article>
  `).join("");return r("gpu-search Findings",`
      <p class="muted"><em>Results are heuristic/retrieval-based, not compiler-verified.</em></p>
      <div class="meta-grid">
        ${o("Queries run",String(e.queriesRun??0))}
        ${o("Total results",String(e.totalResults??0))}
      </div>
      ${t?`<div class="context-list">${t}</div>`:'<p class="empty">No results returned.</p>'}
    `)}function te(e){return e.length===0?r("Recommended Next Steps",'<p class="empty">No recommendations generated.</p>'):r("Recommended Next Steps",`<ul>${e.map(t=>`<li>${s(t)}</li>`).join("")}</ul>`)}async function k(){$.innerHTML='<p class="muted">Loading saved reports...</p>';try{const e=await p("/api/reports");$.innerHTML=e.length?e.map(ue).join(""):'<p class="empty">No saved reports yet. Run a review to create one.</p>',$.querySelectorAll("[data-view]").forEach(t=>{t.addEventListener("click",()=>ne(t.dataset.view??""))}),$.querySelectorAll("[data-delete]").forEach(t=>{t.addEventListener("click",()=>se(t.dataset.delete??""))})}catch(e){$.innerHTML=`<p class="error">Could not load saved reports: ${s(E(e))}</p>`}}async function ne(e){await g("Loading report...",async()=>{const t=await p(`/api/reports/${encodeURIComponent(e)}`);T(t)})}async function se(e){window.confirm("Delete this saved report?")&&await g("Deleting report...",async()=>{await p(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),await k(),y("Report deleted.")})}function T(e){b.clear();const t=e.markdown??"",n=e.gpuSearchContext??null,a=e.roslynContext??null,i=e.llmSummary??"";v("markdown",t),n&&v("context",JSON.stringify(n,null,2)),i&&v("llm",i),m.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Review report</p>
          <h2>${s($e(e))}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="markdown">Copy Markdown</button>
          ${n?'<button class="ghost small" data-copy="context">Copy gpu-search context</button>':""}
          ${i?'<button class="ghost small" data-copy="llm">Copy LLM summary</button>':""}
        </div>
      </header>
      ${ae(e)}
      ${ie(e.findings??[])}
      ${oe(n)}
      ${de(a)}
      ${i?r("LLM Summary",`<div class="llm-summary">${s(i)}</div>`):""}
      ${j(t)}
    </article>
  `,m.querySelectorAll("[data-copy]").forEach(c=>{c.addEventListener("click",()=>F(c.dataset.copy??""))})}function ae(e){const t=e.generatedAtUtc??e.createdAtUtc??e.createdAt;return r("Summary",`
      <div class="meta-grid">
        ${o("Repository",e.repoPath)}
        ${o("Created",t?N(t):void 0)}
        ${o("Changed files",String(e.changedFileCount??e.files?.length??"n/a"))}
        ${o("Provider / mode",e.providerName??e.llmProvider??e.reviewMode??e.analysisMode)}
      </div>
    `)}function ie(e){return e.length===0?r("Findings",'<p class="empty">No findings returned.</p>'):r("Findings",`<div class="finding-list">${e.map(re).join("")}</div>`)}function re(e){const t=w(e.severity),n=e.path??e.filePath,a=P(e);return`
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${t.toLowerCase()}">${t}</span>
        ${e.category?`<span class="badge">${s(e.category)}</span>`:""}
        ${e.ruleId?`<span class="muted mono">${s(e.ruleId)}</span>`:""}
      </div>
      <h3>${s(e.title??e.message??"Review finding")}</h3>
      ${n?`<p class="mono muted">${s(n)}${a?`:${s(a)}`:""}</p>`:""}
      ${e.description?`<p>${s(e.description)}</p>`:""}
    </article>
  `}function oe(e){if(!e)return r("gpu-search Context",'<p class="empty">No gpu-search context was included in this report.</p>');if(e.wasAvailable===!1)return r("gpu-search Context",`<p class="error">gpu-search unavailable: ${s(e.unavailableReason??"No reason provided.")}</p>`);const t=e.warnings?.length?u("Global warnings",e.warnings):"",n=e.limitations?.length?u("Global limitations",e.limitations):"",a=e.files?.length?e.files.map(ce).join(""):'<p class="empty">No per-file gpu-search context returned.</p>';return r("gpu-search Context",`${t}${n}<div class="context-list">${a}</div>`)}function ce(e){const t=e.dependencyImpact,n=me(t?.impactedFiles??e.impactedFiles,t?.directImporters),a=t?.warnings??e.warnings??[],i=t?.limitations??e.limitations??[],c=e.skeleton,l=typeof c=="string"?c:c?.content;return`
    <article class="context-card">
      <div class="row">
        <h3 class="mono">${s(e.filePath??e.path??"Unknown file")}</h3>
        <div class="actions compact">
          ${A(t?.confidence??e.confidence,"confidence")}
          ${A(t?.analysisMode??e.analysisMode,"mode")}
        </div>
      </div>
      ${e.error?`<p class="error">${s(e.error)}</p>`:""}
      <p class="muted">${s(t?.summary??he(t,n))}</p>
      ${n.length?pe(n):""}
      ${a.length?u("Warnings",a):""}
      ${i.length?u("Limitations",i):""}
      ${le(e.relatedResults??[])}
      ${l?`<details><summary>Skeleton preview${typeof c=="object"&&c?.language?` · ${s(c.language)}`:""}</summary><pre>${s(l)}</pre></details>`:""}
    </article>
  `}function le(e){return e.length===0?'<p class="muted">No related search results.</p>':`
    <div class="related-list">
      <h4>Related search results</h4>
      ${e.map(t=>`
            <article class="related-card">
              <div class="row">
                <strong class="mono">${s(t.file??t.filePath??"Unknown file")}${P(t)?`:${s(P(t))}`:""}</strong>
                <span class="badge">${s(t.engine??"search")}${t.score!==null&&t.score!==void 0?` · ${t.score.toFixed(2)}`:""}</span>
              </div>
              ${t.reason?`<p class="muted">${s(t.reason)}</p>`:""}
              ${t.snippet?`<pre>${s(t.snippet)}</pre>`:""}
            </article>
          `).join("")}
    </div>
  `}function de(e){if(!e)return"";if(!e.success||!e.changedSymbols?.length&&e.errorMessage)return r("Roslyn Reference Context",`<p class="error">Roslyn reference analysis was unavailable. Review continued with deterministic and gpu-search context.</p>
       ${e.errorMessage?`<p class="muted">${s(e.errorMessage)}</p>`:""}`);const t=e.changedSymbols??[];if(t.length===0)return r("Roslyn Reference Context",'<p class="empty">No changed C# symbols were matched.</p>');const n=e.symbolReferences??[],a=t.map(l=>{const R=n.filter(d=>d.symbolName===l.name&&d.symbolFullName===l.fullName&&!d.isDefinition),B=R.slice(0,10).map(d=>{const D=d.containingSymbol?` <span class="muted">— in ${s(d.containingSymbol)}</span>`:"";return`<li class="mono">${s(d.filePath??"")}:${d.line??"?"}${D}</li>`}).join("");return`
        <article class="context-card">
          <div class="row">
            <h3 class="mono">${s(l.name??"Unknown")}</h3>
            <span class="badge">${s(l.kind??"symbol")}</span>
          </div>
          <div class="meta-grid">
            ${o("Project",l.projectName)}
            ${o("Definition",l.filePath?`${l.filePath}:${l.line??"?"}`:void 0)}
            ${o("References",String(R.length))}
          </div>
          ${R.length?`<div class="mini-block"><h4>References</h4><ul>${B}</ul></div>`:""}
        </article>
      `}).join(""),i=e.warnings?.length?u("Warnings",e.warnings):"",c=e.workspacePath?`<p class="muted mono">${s(e.workspacePath)}${e.workspaceKind?` (${s(e.workspaceKind)})`:""}</p>`:"";return r("Roslyn Reference Context",`${c}${i}<div class="context-list">${a}</div>`)}function j(e){return`
    <details class="raw-markdown">
      <summary>Raw Markdown</summary>
      <pre>${s(e||"No Markdown returned.")}</pre>
    </details>
  `}async function g(e,t){C(e);try{await t(),C("Ready")}catch(n){C("Error"),y(E(n),!0)}}async function p(e,t){const n=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!n.ok){const a=await n.text();throw new Error(a||`${n.status} ${n.statusText}`)}if(n.status!==204)return await n.json()}function ue(e){const t=e.reportId??e.id??"";return`
    <article class="report">
      <div>
        <strong>${s(e.repoPath??"Unknown repository")}</strong>
        <span>${N(e.generatedAtUtc??e.createdAtUtc)} · ${e.changedFileCount??"n/a"} files · ${s(e.llmProvider??e.providerName??"n/a")}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${s(t)}">View</button>
        <button class="danger small" data-delete="${s(t)}">Delete</button>
      </div>
    </article>
  `}function I(e,t){return`
    <div class="result-card">
      <h3>${s(e)}</h3>
      <ul>${t.map(n=>`<li>${s(n)}</li>`).join("")}</ul>
    </div>
  `}function r(e,t){return`
    <section class="viewer-section">
      <h2>${s(e)}</h2>
      ${t}
    </section>
  `}function o(e,t){return`
    <div class="meta-item">
      <span>${s(e)}</span>
      <strong>${s(t||"n/a")}</strong>
    </div>
  `}function pe(e){return`
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${e.map(t=>{const n=t.file??t.filePath??t.path??"Unknown file",a=t.hops!==null&&t.hops!==void 0?` · ${t.hops} hop${t.hops===1?"":"s"}`:"",i=t.reason?` <span class="reason">— ${s(t.reason)} <em>(heuristic)</em></span>`:"";return`<li class="mono">${s(n)}${a}${i}</li>`}).join("")}
      </ul>
    </div>
  `}function me(e,t){return Array.isArray(e)&&e.length>0?e.map(n=>typeof n=="string"?{file:n,hops:1}:n):(t??[]).map(n=>({file:n,hops:1}))}function u(e,t){return`
    <details>
      <summary>${s(e)} (${t.length})</summary>
      <ul>${t.map(n=>`<li>${s(n)}</li>`).join("")}</ul>
    </details>
  `}function A(e,t=""){return e?`<span class="badge ${t}">${s(e)}</span>`:""}function ge(e){return`${e.status??"M"} ${e.path??e.filePath??"Unknown file"} (+${e.additions??0}/-${e.deletions??0})`}function he(e,t){return e?`${e.totalImpacted??t.length} impacted file(s) identified.`:"No dependency impact summary returned."}function $e(e){return e.reportId??e.id??"Review report"}function P(e){return e.lineStart&&e.lineEnd?e.lineStart===e.lineEnd?String(e.lineStart):`${e.lineStart}-${e.lineEnd}`:e.lineStart?String(e.lineStart):e.line?String(e.line):""}function w(e){const t=(e??"Info").toLowerCase();return["low","medium","high","critical"].includes(t)?t[0].toUpperCase()+t.slice(1):"Info"}function v(e,t){t&&b.set(e,t)}async function F(e){const t=b.get(e);if(!t){y("Nothing to copy.",!0);return}try{if(!navigator.clipboard)throw new Error("Clipboard API is not available.");await navigator.clipboard.writeText(t),y("Copied.")}catch(n){y(`Copy failed: ${E(n)}`,!0)}}function h(e,t){f(e).addEventListener("click",()=>{t()})}function x(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function f(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function C(e){O.textContent=e}function y(e,t=!1){L.textContent=e,L.className=t?"toast show error":"toast show",window.setTimeout(()=>{L.className="toast"},2600)}function N(e){if(!e)return"n/a";const t=new Date(e);return Number.isNaN(t.getTime())?e:t.toLocaleString()}function E(e){return e instanceof Error?e.message:String(e)}function s(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
