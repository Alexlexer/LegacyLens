(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const a of document.querySelectorAll('link[rel="modulepreload"]'))r(a);new MutationObserver(a=>{for(const o of a)if(o.type==="childList")for(const c of o.addedNodes)c.tagName==="LINK"&&c.rel==="modulepreload"&&r(c)}).observe(document,{childList:!0,subtree:!0});function s(a){const o={};return a.integrity&&(o.integrity=a.integrity),a.referrerPolicy&&(o.referrerPolicy=a.referrerPolicy),a.crossOrigin==="use-credentials"?o.credentials="include":a.crossOrigin==="anonymous"?o.credentials="omit":o.credentials="same-origin",o}function r(a){if(a.ep)return;a.ep=!0;const o=s(a);fetch(a.href,o)}})();const A=document.querySelector("#app");if(!A)throw new Error("App root was not found.");A.innerHTML=`
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
          <button id="diAnalyzeButton" class="secondary">DI analysis</button>
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
        <h2>Report viewer</h2>
        <span id="statusPill" class="pill">Ready</span>
      </div>
      <div id="toast" class="toast" aria-live="polite"></div>
      <div id="output" class="output muted">Run a preview, review, or analysis to see results.</div>
    </section>
  </main>
`;const b=F("repoPath"),O=F("useLlm"),h=v("output"),U=v("statusPill"),g=v("reports"),P=v("toast"),N=new Map;f("statusButton",x);f("previewButton",H);f("reviewButton",z);f("analyzeButton",q);f("diAnalyzeButton",G);f("refreshReportsButton",R);R();async function x(){await m("Checking gpu-search...",async()=>{const e=await p("/api/search/status");h.innerHTML=I(e.isAvailable?"gpu-search is available":"gpu-search is unavailable",[`Health: ${e.health?.status??"n/a"}`,`Backend: ${e.stats?.backend??"n/a"}`,`Device: ${e.stats?.device??"n/a"}`,`Indexed files: ${e.stats?.indexedFileCount??"n/a"}`,e.error?`Error: ${e.error}`:""].filter(Boolean))})}async function H(){await m("Loading diff preview...",async()=>{const e=await p("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:b.value})});h.innerHTML=`
      ${I(`${e.changedFileCount??0} changed file(s)`,(e.files??[]).map(ie))}
      <pre>${n(e.diff||"No diff content.")}</pre>
    `})}async function z(){await m("Generating review...",async()=>{const e=await p("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:b.value,useLlm:O.checked})});j(e),await R()})}async function q(){await m("Running .NET analysis...",async()=>{const e=await p("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:b.value,limitPerPreset:8})});h.innerHTML=I(`${e.findings?.length??0} preset finding(s)`,(e.findings??[]).map(t=>`${t.presetId??"preset"} · ${t.filePath??"unknown file"}${t.line?`:${t.line}`:""} · ${t.snippet??""}`))})}async function G(){await m("Running DI analysis...",async()=>{const e=await p("/api/dotnet/di/analyze",{method:"POST",body:JSON.stringify({repoPath:b.value})});h.innerHTML=J(e)})}function J(e){if(!e.success)return l("DI Analysis",`<p class="error">DI analysis failed: ${n(e.errorMessage??"Unknown error")}</p>`);const t=e.registrations??[],s=e.findings??[],r=e.warnings??[],a=s.map(i=>{const d=D(i.severity),M=i.filePath?`${n(i.filePath)}${i.line?`:${i.line}`:""}`:"";return`
      <article class="finding-card">
        <div class="row">
          <span class="badge severity-${d.toLowerCase()}">${d}</span>
          <span class="muted mono">${n(i.code??"")}</span>
        </div>
        <p>${n(i.message??"")}</p>
        ${M?`<p class="mono muted">${M}</p>`:""}
      </article>
    `}),o={};for(const i of t){const d=i.lifetime??"Unknown";o[d]=(o[d]??0)+1}const c=Object.entries(o).map(([i,d])=>`${d} ${i}`),$=t.slice(0,50).map(i=>`<li class="mono">${n(i.lifetime??"?")} · ${n(i.serviceType??"(no type)")}${i.implementationType?` → ${n(i.implementationType)}`:""} <span class="muted">${n(i.filePath??"")}${i.line?`:${i.line}`:""}</span></li>`),k=e.workspacePath?`<p class="muted mono">${n(e.workspacePath)}${e.workspaceKind?` (${n(e.workspaceKind)})`:""}</p>`:"";return`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">DI Analysis</p>
          <h2>${c.length?c.join(", ")+" registrations":"No registrations found"}</h2>
        </div>
      </header>
      ${k}
      ${s.length?l("Findings",`<div class="finding-list">${a.join("")}</div>`):l("Findings",'<p class="empty">No DI findings.</p>')}
      ${t.length?l("Registrations",`<ul>${$.join("")}</ul>`):""}
      ${r.length?l("Warnings",`<ul>${r.map(i=>`<li>${n(i)}</li>`).join("")}</ul>`):""}
    </article>
  `}async function R(){g.innerHTML='<p class="muted">Loading saved reports...</p>';try{const e=await p("/api/reports");g.innerHTML=e.length?e.map(ne).join(""):'<p class="empty">No saved reports yet. Run a review to create one.</p>',g.querySelectorAll("[data-view]").forEach(t=>{t.addEventListener("click",()=>K(t.dataset.view??""))}),g.querySelectorAll("[data-delete]").forEach(t=>{t.addEventListener("click",()=>W(t.dataset.delete??""))})}catch(e){g.innerHTML=`<p class="error">Could not load saved reports: ${n(E(e))}</p>`}}async function K(e){await m("Loading report...",async()=>{const t=await p(`/api/reports/${encodeURIComponent(e)}`);j(t)})}async function W(e){window.confirm("Delete this saved report?")&&await m("Deleting report...",async()=>{await p(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),await R(),y("Report deleted.")})}function j(e){N.clear();const t=e.markdown??"",s=e.gpuSearchContext??null,r=e.roslynContext??null,a=e.llmSummary??"";L("markdown",t),s&&L("context",JSON.stringify(s,null,2)),a&&L("llm",a),h.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Review report</p>
          <h2>${n(re(e))}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="markdown">Copy Markdown</button>
          ${s?'<button class="ghost small" data-copy="context">Copy gpu-search context</button>':""}
          ${a?'<button class="ghost small" data-copy="llm">Copy LLM summary</button>':""}
        </div>
      </header>
      ${V(e)}
      ${Q(e.findings??[])}
      ${Y(s)}
      ${ee(r)}
      ${a?l("LLM Summary",`<div class="llm-summary">${n(a)}</div>`):""}
      ${te(t)}
    </article>
  `,h.querySelectorAll("[data-copy]").forEach(o=>{o.addEventListener("click",()=>ce(o.dataset.copy??""))})}function V(e){const t=e.generatedAtUtc??e.createdAtUtc??e.createdAt;return l("Summary",`
      <div class="meta-grid">
        ${u("Repository",e.repoPath)}
        ${u("Created",t?B(t):void 0)}
        ${u("Changed files",String(e.changedFileCount??e.files?.length??"n/a"))}
        ${u("Provider / mode",e.providerName??e.llmProvider??e.reviewMode??e.analysisMode)}
      </div>
    `)}function Q(e){return e.length===0?l("Findings",'<p class="empty">No findings returned.</p>'):l("Findings",`<div class="finding-list">${e.map(X).join("")}</div>`)}function X(e){const t=D(e.severity),s=e.path??e.filePath,r=C(e);return`
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${t.toLowerCase()}">${t}</span>
        ${e.category?`<span class="badge">${n(e.category)}</span>`:""}
        ${e.ruleId?`<span class="muted mono">${n(e.ruleId)}</span>`:""}
      </div>
      <h3>${n(e.title??e.message??"Review finding")}</h3>
      ${s?`<p class="mono muted">${n(s)}${r?`:${n(r)}`:""}</p>`:""}
      ${e.description?`<p>${n(e.description)}</p>`:""}
    </article>
  `}function Y(e){if(!e)return l("gpu-search Context",'<p class="empty">No gpu-search context was included in this report.</p>');if(e.wasAvailable===!1)return l("gpu-search Context",`<p class="error">gpu-search unavailable: ${n(e.unavailableReason??"No reason provided.")}</p>`);const t=e.warnings?.length?w("Global warnings",e.warnings):"",s=e.limitations?.length?w("Global limitations",e.limitations):"",r=e.files?.length?e.files.map(Z).join(""):'<p class="empty">No per-file gpu-search context returned.</p>';return l("gpu-search Context",`${t}${s}<div class="context-list">${r}</div>`)}function Z(e){const t=e.dependencyImpact,s=ae(t?.impactedFiles??e.impactedFiles,t?.directImporters),r=t?.warnings??e.warnings??[],a=t?.limitations??e.limitations??[],o=e.skeleton,c=typeof o=="string"?o:o?.content;return`
    <article class="context-card">
      <div class="row">
        <h3 class="mono">${n(e.filePath??e.path??"Unknown file")}</h3>
        <div class="actions compact">
          ${T(t?.confidence??e.confidence,"confidence")}
          ${T(t?.analysisMode??e.analysisMode,"mode")}
        </div>
      </div>
      ${e.error?`<p class="error">${n(e.error)}</p>`:""}
      <p class="muted">${n(t?.summary??oe(t,s))}</p>
      ${s.length?se(s):""}
      ${r.length?w("Warnings",r):""}
      ${a.length?w("Limitations",a):""}
      ${_(e.relatedResults??[])}
      ${c?`<details><summary>Skeleton preview${typeof o=="object"&&o?.language?` · ${n(o.language)}`:""}</summary><pre>${n(c)}</pre></details>`:""}
    </article>
  `}function _(e){return e.length===0?'<p class="muted">No related search results.</p>':`
    <div class="related-list">
      <h4>Related search results</h4>
      ${e.map(t=>`
            <article class="related-card">
              <div class="row">
                <strong class="mono">${n(t.file??t.filePath??"Unknown file")}${C(t)?`:${n(C(t))}`:""}</strong>
                <span class="badge">${n(t.engine??"search")}${t.score!==null&&t.score!==void 0?` · ${t.score.toFixed(2)}`:""}</span>
              </div>
              ${t.reason?`<p class="muted">${n(t.reason)}</p>`:""}
              ${t.snippet?`<pre>${n(t.snippet)}</pre>`:""}
            </article>
          `).join("")}
    </div>
  `}function ee(e){if(!e)return"";if(!e.success||!e.changedSymbols?.length&&e.errorMessage)return l("Roslyn Reference Context",`<p class="error">Roslyn reference analysis was unavailable. Review continued with deterministic and gpu-search context.</p>
       ${e.errorMessage?`<p class="muted">${n(e.errorMessage)}</p>`:""}`);const t=e.changedSymbols??[];if(t.length===0)return l("Roslyn Reference Context",'<p class="empty">No changed C# symbols were matched.</p>');const s=e.symbolReferences??[],r=t.map(c=>{const $=s.filter(i=>i.symbolName===c.name&&i.symbolFullName===c.fullName&&!i.isDefinition),k=$.slice(0,10).map(i=>{const d=i.containingSymbol?` <span class="muted">— in ${n(i.containingSymbol)}</span>`:"";return`<li class="mono">${n(i.filePath??"")}:${i.line??"?"}${d}</li>`}).join("");return`
        <article class="context-card">
          <div class="row">
            <h3 class="mono">${n(c.name??"Unknown")}</h3>
            <span class="badge">${n(c.kind??"symbol")}</span>
          </div>
          <div class="meta-grid">
            ${u("Project",c.projectName)}
            ${u("Definition",c.filePath?`${c.filePath}:${c.line??"?"}`:void 0)}
            ${u("References",String($.length))}
          </div>
          ${$.length?`<div class="mini-block"><h4>References</h4><ul>${k}</ul></div>`:""}
        </article>
      `}).join(""),a=e.warnings?.length?w("Warnings",e.warnings):"",o=e.workspacePath?`<p class="muted mono">${n(e.workspacePath)}${e.workspaceKind?` (${n(e.workspaceKind)})`:""}</p>`:"";return l("Roslyn Reference Context",`${o}${a}<div class="context-list">${r}</div>`)}function te(e){return`
    <details class="raw-markdown">
      <summary>Raw Markdown</summary>
      <pre>${n(e||"No Markdown returned.")}</pre>
    </details>
  `}async function m(e,t){S(e);try{await t(),S("Ready")}catch(s){S("Error"),y(E(s),!0)}}async function p(e,t){const s=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!s.ok){const r=await s.text();throw new Error(r||`${s.status} ${s.statusText}`)}if(s.status!==204)return await s.json()}function ne(e){const t=e.reportId??e.id??"";return`
    <article class="report">
      <div>
        <strong>${n(e.repoPath??"Unknown repository")}</strong>
        <span>${B(e.generatedAtUtc??e.createdAtUtc)} · ${e.changedFileCount??"n/a"} files · ${n(e.llmProvider??e.providerName??"n/a")}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${n(t)}">View</button>
        <button class="danger small" data-delete="${n(t)}">Delete</button>
      </div>
    </article>
  `}function I(e,t){return`
    <div class="result-card">
      <h3>${n(e)}</h3>
      <ul>${t.map(s=>`<li>${n(s)}</li>`).join("")}</ul>
    </div>
  `}function l(e,t){return`
    <section class="viewer-section">
      <h2>${n(e)}</h2>
      ${t}
    </section>
  `}function u(e,t){return`
    <div class="meta-item">
      <span>${n(e)}</span>
      <strong>${n(t||"n/a")}</strong>
    </div>
  `}function se(e){return`
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${e.map(t=>{const s=t.file??t.filePath??t.path??"Unknown file",r=t.hops!==null&&t.hops!==void 0?` · ${t.hops} hop${t.hops===1?"":"s"}`:"",a=t.reason?` <span class="reason">— ${n(t.reason)} <em>(heuristic)</em></span>`:"";return`<li class="mono">${n(s)}${r}${a}</li>`}).join("")}
      </ul>
    </div>
  `}function ae(e,t){return Array.isArray(e)&&e.length>0?e.map(s=>typeof s=="string"?{file:s,hops:1}:s):(t??[]).map(s=>({file:s,hops:1}))}function w(e,t){return`
    <details>
      <summary>${n(e)} (${t.length})</summary>
      <ul>${t.map(s=>`<li>${n(s)}</li>`).join("")}</ul>
    </details>
  `}function T(e,t=""){return e?`<span class="badge ${t}">${n(e)}</span>`:""}function ie(e){return`${e.status??"M"} ${e.path??e.filePath??"Unknown file"} (+${e.additions??0}/-${e.deletions??0})`}function oe(e,t){return e?`${e.totalImpacted??t.length} impacted file(s) identified.`:"No dependency impact summary returned."}function re(e){return e.reportId??e.id??"Review report"}function C(e){return e.lineStart&&e.lineEnd?e.lineStart===e.lineEnd?String(e.lineStart):`${e.lineStart}-${e.lineEnd}`:e.lineStart?String(e.lineStart):e.line?String(e.line):""}function D(e){const t=(e??"Info").toLowerCase();return["low","medium","high","critical"].includes(t)?t[0].toUpperCase()+t.slice(1):"Info"}function L(e,t){t&&N.set(e,t)}async function ce(e){const t=N.get(e);if(!t){y("Nothing to copy.",!0);return}try{if(!navigator.clipboard)throw new Error("Clipboard API is not available.");await navigator.clipboard.writeText(t),y("Copied.")}catch(s){y(`Copy failed: ${E(s)}`,!0)}}function f(e,t){v(e).addEventListener("click",()=>{t()})}function F(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function v(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function S(e){U.textContent=e}function y(e,t=!1){P.textContent=e,P.className=t?"toast show error":"toast show",window.setTimeout(()=>{P.className="toast"},2600)}function B(e){if(!e)return"n/a";const t=new Date(e);return Number.isNaN(t.getTime())?e:t.toLocaleString()}function E(e){return e instanceof Error?e.message:String(e)}function n(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
