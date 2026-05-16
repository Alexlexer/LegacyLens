(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const s of document.querySelectorAll('link[rel="modulepreload"]'))i(s);new MutationObserver(s=>{for(const r of s)if(r.type==="childList")for(const o of r.addedNodes)o.tagName==="LINK"&&o.rel==="modulepreload"&&i(o)}).observe(document,{childList:!0,subtree:!0});function a(s){const r={};return s.integrity&&(r.integrity=s.integrity),s.referrerPolicy&&(r.referrerPolicy=s.referrerPolicy),s.crossOrigin==="use-credentials"?r.credentials="include":s.crossOrigin==="anonymous"?r.credentials="omit":r.credentials="same-origin",r}function i(s){if(s.ep)return;s.ep=!0;const r=a(s);fetch(s.href,r)}})();const M=document.querySelector("#app");if(!M)throw new Error("App root was not found.");M.innerHTML=`
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
`;const k=T("repoPath"),B=T("useLlm"),f=w("output"),x=w("statusPill"),m=w("reports"),b=w("toast"),C=new Map;g("statusButton",O);g("previewButton",U);g("reviewButton",D);g("analyzeButton",H);g("refreshReportsButton",y);y();async function O(){await u("Checking gpu-search...",async()=>{const e=await p("/api/search/status");f.innerHTML=P(e.isAvailable?"gpu-search is available":"gpu-search is unavailable",[`Health: ${e.health?.status??"n/a"}`,`Backend: ${e.stats?.backend??"n/a"}`,`Device: ${e.stats?.device??"n/a"}`,`Indexed files: ${e.stats?.indexedFileCount??"n/a"}`,e.error?`Error: ${e.error}`:""].filter(Boolean))})}async function U(){await u("Loading diff preview...",async()=>{const e=await p("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:k.value})});f.innerHTML=`
      ${P(`${e.changedFileCount??0} changed file(s)`,(e.files??[]).map(te))}
      <pre>${n(e.diff||"No diff content.")}</pre>
    `})}async function D(){await u("Generating review...",async()=>{const e=await p("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:k.value,useLlm:B.checked})});I(e),await y()})}async function H(){await u("Running .NET analysis...",async()=>{const e=await p("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:k.value,limitPerPreset:8})});f.innerHTML=P(`${e.findings?.length??0} preset finding(s)`,(e.findings??[]).map(t=>`${t.presetId??"preset"} · ${t.filePath??"unknown file"}${t.line?`:${t.line}`:""} · ${t.snippet??""}`))})}async function y(){m.innerHTML='<p class="muted">Loading saved reports...</p>';try{const e=await p("/api/reports");m.innerHTML=e.length?e.map(Z).join(""):'<p class="empty">No saved reports yet. Run a review to create one.</p>',m.querySelectorAll("[data-view]").forEach(t=>{t.addEventListener("click",()=>q(t.dataset.view??""))}),m.querySelectorAll("[data-delete]").forEach(t=>{t.addEventListener("click",()=>z(t.dataset.delete??""))})}catch(e){m.innerHTML=`<p class="error">Could not load saved reports: ${n(N(e))}</p>`}}async function q(e){await u("Loading report...",async()=>{const t=await p(`/api/reports/${encodeURIComponent(e)}`);I(t)})}async function z(e){window.confirm("Delete this saved report?")&&await u("Deleting report...",async()=>{await p(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),await y(),h("Report deleted.")})}function I(e){C.clear();const t=e.markdown??"",a=e.gpuSearchContext??null,i=e.roslynContext??null,s=e.llmSummary??"";R("markdown",t),a&&R("context",JSON.stringify(a,null,2)),s&&R("llm",s),f.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Review report</p>
          <h2>${n(ae(e))}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="markdown">Copy Markdown</button>
          ${a?'<button class="ghost small" data-copy="context">Copy gpu-search context</button>':""}
          ${s?'<button class="ghost small" data-copy="llm">Copy LLM summary</button>':""}
        </div>
      </header>
      ${G(e)}
      ${J(e.findings??[])}
      ${W(a)}
      ${X(i)}
      ${s?c("LLM Summary",`<div class="llm-summary">${n(s)}</div>`):""}
      ${Y(t)}
    </article>
  `,f.querySelectorAll("[data-copy]").forEach(r=>{r.addEventListener("click",()=>re(r.dataset.copy??""))})}function G(e){const t=e.generatedAtUtc??e.createdAtUtc??e.createdAt;return c("Summary",`
      <div class="meta-grid">
        ${d("Repository",e.repoPath)}
        ${d("Created",t?A(t):void 0)}
        ${d("Changed files",String(e.changedFileCount??e.files?.length??"n/a"))}
        ${d("Provider / mode",e.providerName??e.llmProvider??e.reviewMode??e.analysisMode)}
      </div>
    `)}function J(e){return e.length===0?c("Findings",'<p class="empty">No findings returned.</p>'):c("Findings",`<div class="finding-list">${e.map(K).join("")}</div>`)}function K(e){const t=se(e.severity),a=e.path??e.filePath,i=S(e);return`
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${t.toLowerCase()}">${t}</span>
        ${e.category?`<span class="badge">${n(e.category)}</span>`:""}
        ${e.ruleId?`<span class="muted mono">${n(e.ruleId)}</span>`:""}
      </div>
      <h3>${n(e.title??e.message??"Review finding")}</h3>
      ${a?`<p class="mono muted">${n(a)}${i?`:${n(i)}`:""}</p>`:""}
      ${e.description?`<p>${n(e.description)}</p>`:""}
    </article>
  `}function W(e){if(!e)return c("gpu-search Context",'<p class="empty">No gpu-search context was included in this report.</p>');if(e.wasAvailable===!1)return c("gpu-search Context",`<p class="error">gpu-search unavailable: ${n(e.unavailableReason??"No reason provided.")}</p>`);const t=e.warnings?.length?$("Global warnings",e.warnings):"",a=e.limitations?.length?$("Global limitations",e.limitations):"",i=e.files?.length?e.files.map(V).join(""):'<p class="empty">No per-file gpu-search context returned.</p>';return c("gpu-search Context",`${t}${a}<div class="context-list">${i}</div>`)}function V(e){const t=e.dependencyImpact,a=ee(t?.impactedFiles??e.impactedFiles,t?.directImporters),i=t?.warnings??e.warnings??[],s=t?.limitations??e.limitations??[],r=e.skeleton,o=typeof r=="string"?r:r?.content;return`
    <article class="context-card">
      <div class="row">
        <h3 class="mono">${n(e.filePath??e.path??"Unknown file")}</h3>
        <div class="actions compact">
          ${E(t?.confidence??e.confidence,"confidence")}
          ${E(t?.analysisMode??e.analysisMode,"mode")}
        </div>
      </div>
      ${e.error?`<p class="error">${n(e.error)}</p>`:""}
      <p class="muted">${n(t?.summary??ne(t,a))}</p>
      ${a.length?_(a):""}
      ${i.length?$("Warnings",i):""}
      ${s.length?$("Limitations",s):""}
      ${Q(e.relatedResults??[])}
      ${o?`<details><summary>Skeleton preview${typeof r=="object"&&r?.language?` · ${n(r.language)}`:""}</summary><pre>${n(o)}</pre></details>`:""}
    </article>
  `}function Q(e){return e.length===0?'<p class="muted">No related search results.</p>':`
    <div class="related-list">
      <h4>Related search results</h4>
      ${e.map(t=>`
            <article class="related-card">
              <div class="row">
                <strong class="mono">${n(t.file??t.filePath??"Unknown file")}${S(t)?`:${n(S(t))}`:""}</strong>
                <span class="badge">${n(t.engine??"search")}${t.score!==null&&t.score!==void 0?` · ${t.score.toFixed(2)}`:""}</span>
              </div>
              ${t.reason?`<p class="muted">${n(t.reason)}</p>`:""}
              ${t.snippet?`<pre>${n(t.snippet)}</pre>`:""}
            </article>
          `).join("")}
    </div>
  `}function X(e){if(!e)return"";if(!e.success||!e.changedSymbols?.length&&e.errorMessage)return c("Roslyn Reference Context",`<p class="error">Roslyn reference analysis was unavailable. Review continued with deterministic and gpu-search context.</p>
       ${e.errorMessage?`<p class="muted">${n(e.errorMessage)}</p>`:""}`);const t=e.changedSymbols??[];if(t.length===0)return c("Roslyn Reference Context",'<p class="empty">No changed C# symbols were matched.</p>');const a=e.symbolReferences??[],i=t.map(o=>{const v=a.filter(l=>l.symbolName===o.name&&l.symbolFullName===o.fullName&&!l.isDefinition),F=v.slice(0,10).map(l=>{const j=l.containingSymbol?` <span class="muted">— in ${n(l.containingSymbol)}</span>`:"";return`<li class="mono">${n(l.filePath??"")}:${l.line??"?"}${j}</li>`}).join("");return`
        <article class="context-card">
          <div class="row">
            <h3 class="mono">${n(o.name??"Unknown")}</h3>
            <span class="badge">${n(o.kind??"symbol")}</span>
          </div>
          <div class="meta-grid">
            ${d("Project",o.projectName)}
            ${d("Definition",o.filePath?`${o.filePath}:${o.line??"?"}`:void 0)}
            ${d("References",String(v.length))}
          </div>
          ${v.length?`<div class="mini-block"><h4>References</h4><ul>${F}</ul></div>`:""}
        </article>
      `}).join(""),s=e.warnings?.length?$("Warnings",e.warnings):"",r=e.workspacePath?`<p class="muted mono">${n(e.workspacePath)}${e.workspaceKind?` (${n(e.workspaceKind)})`:""}</p>`:"";return c("Roslyn Reference Context",`${r}${s}<div class="context-list">${i}</div>`)}function Y(e){return`
    <details class="raw-markdown">
      <summary>Raw Markdown</summary>
      <pre>${n(e||"No Markdown returned.")}</pre>
    </details>
  `}async function u(e,t){L(e);try{await t(),L("Ready")}catch(a){L("Error"),h(N(a),!0)}}async function p(e,t){const a=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!a.ok){const i=await a.text();throw new Error(i||`${a.status} ${a.statusText}`)}if(a.status!==204)return await a.json()}function Z(e){const t=e.reportId??e.id??"";return`
    <article class="report">
      <div>
        <strong>${n(e.repoPath??"Unknown repository")}</strong>
        <span>${A(e.generatedAtUtc??e.createdAtUtc)} · ${e.changedFileCount??"n/a"} files · ${n(e.llmProvider??e.providerName??"n/a")}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${n(t)}">View</button>
        <button class="danger small" data-delete="${n(t)}">Delete</button>
      </div>
    </article>
  `}function P(e,t){return`
    <div class="result-card">
      <h3>${n(e)}</h3>
      <ul>${t.map(a=>`<li>${n(a)}</li>`).join("")}</ul>
    </div>
  `}function c(e,t){return`
    <section class="viewer-section">
      <h2>${n(e)}</h2>
      ${t}
    </section>
  `}function d(e,t){return`
    <div class="meta-item">
      <span>${n(e)}</span>
      <strong>${n(t||"n/a")}</strong>
    </div>
  `}function _(e){return`
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${e.map(t=>{const a=t.file??t.filePath??t.path??"Unknown file",i=t.hops!==null&&t.hops!==void 0?` · ${t.hops} hop${t.hops===1?"":"s"}`:"",s=t.reason?` <span class="reason">— ${n(t.reason)} <em>(heuristic)</em></span>`:"";return`<li class="mono">${n(a)}${i}${s}</li>`}).join("")}
      </ul>
    </div>
  `}function ee(e,t){return Array.isArray(e)&&e.length>0?e.map(a=>typeof a=="string"?{file:a,hops:1}:a):(t??[]).map(a=>({file:a,hops:1}))}function $(e,t){return`
    <details>
      <summary>${n(e)} (${t.length})</summary>
      <ul>${t.map(a=>`<li>${n(a)}</li>`).join("")}</ul>
    </details>
  `}function E(e,t=""){return e?`<span class="badge ${t}">${n(e)}</span>`:""}function te(e){return`${e.status??"M"} ${e.path??e.filePath??"Unknown file"} (+${e.additions??0}/-${e.deletions??0})`}function ne(e,t){return e?`${e.totalImpacted??t.length} impacted file(s) identified.`:"No dependency impact summary returned."}function ae(e){return e.reportId??e.id??"Review report"}function S(e){return e.lineStart&&e.lineEnd?e.lineStart===e.lineEnd?String(e.lineStart):`${e.lineStart}-${e.lineEnd}`:e.lineStart?String(e.lineStart):e.line?String(e.line):""}function se(e){const t=(e??"Info").toLowerCase();return["low","medium","high","critical"].includes(t)?t[0].toUpperCase()+t.slice(1):"Info"}function R(e,t){t&&C.set(e,t)}async function re(e){const t=C.get(e);if(!t){h("Nothing to copy.",!0);return}try{if(!navigator.clipboard)throw new Error("Clipboard API is not available.");await navigator.clipboard.writeText(t),h("Copied.")}catch(a){h(`Copy failed: ${N(a)}`,!0)}}function g(e,t){w(e).addEventListener("click",()=>{t()})}function T(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function w(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function L(e){x.textContent=e}function h(e,t=!1){b.textContent=e,b.className=t?"toast show error":"toast show",window.setTimeout(()=>{b.className="toast"},2600)}function A(e){if(!e)return"n/a";const t=new Date(e);return Number.isNaN(t.getTime())?e:t.toLocaleString()}function N(e){return e instanceof Error?e.message:String(e)}function n(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
