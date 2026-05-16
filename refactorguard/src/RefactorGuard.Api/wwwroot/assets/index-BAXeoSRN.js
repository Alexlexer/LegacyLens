(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const s of document.querySelectorAll('link[rel="modulepreload"]'))r(s);new MutationObserver(s=>{for(const o of s)if(o.type==="childList")for(const l of o.addedNodes)l.tagName==="LINK"&&l.rel==="modulepreload"&&r(l)}).observe(document,{childList:!0,subtree:!0});function n(s){const o={};return s.integrity&&(o.integrity=s.integrity),s.referrerPolicy&&(o.referrerPolicy=s.referrerPolicy),s.crossOrigin==="use-credentials"?o.credentials="include":s.crossOrigin==="anonymous"?o.credentials="omit":o.credentials="same-origin",o}function r(s){if(s.ep)return;s.ep=!0;const o=n(s);fetch(s.href,o)}})();const E=document.querySelector("#app");if(!E)throw new Error("App root was not found.");E.innerHTML=`
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
        <h2>Report viewer</h2>
        <span id="statusPill" class="pill">Ready</span>
      </div>
      <div id="toast" class="toast" aria-live="polite"></div>
      <div id="output" class="output muted">Run a preview, review, or analysis to see results.</div>
    </section>
  </main>
`;const S=M("repoPath"),I=M("useLlm"),m=f("output"),A=f("statusPill"),p=f("reports"),$=f("toast"),P=new Map;h("statusButton",x);h("previewButton",F);h("reviewButton",B);h("analyzeButton",O);h("refreshReportsButton",g);g();async function x(){await d("Checking gpu-search...",async()=>{const e=await c("/api/search/status");m.innerHTML=k(e.isAvailable?"gpu-search is available":"gpu-search is unavailable",[`Health: ${e.health?.status??"n/a"}`,`Backend: ${e.stats?.backend??"n/a"}`,`Device: ${e.stats?.device??"n/a"}`,`Indexed files: ${e.stats?.indexedFileCount??"n/a"}`,e.error?`Error: ${e.error}`:""].filter(Boolean))})}async function F(){await d("Loading diff preview...",async()=>{const e=await c("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:S.value})});m.innerHTML=`
      ${k(`${e.changedFileCount??0} changed file(s)`,(e.files??[]).map(X))}
      <pre>${a(e.diff||"No diff content.")}</pre>
    `})}async function B(){await d("Generating review...",async()=>{const e=await c("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:S.value,useLlm:I.checked})});N(e),await g()})}async function O(){await d("Running .NET analysis...",async()=>{const e=await c("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:S.value,limitPerPreset:8})});m.innerHTML=k(`${e.findings?.length??0} preset finding(s)`,(e.findings??[]).map(t=>`${t.presetId??"preset"} · ${t.filePath??"unknown file"}${t.line?`:${t.line}`:""} · ${t.snippet??""}`))})}async function g(){p.innerHTML='<p class="muted">Loading saved reports...</p>';try{const e=await c("/api/reports");p.innerHTML=e.length?e.map(V).join(""):'<p class="empty">No saved reports yet. Run a review to create one.</p>',p.querySelectorAll("[data-view]").forEach(t=>{t.addEventListener("click",()=>U(t.dataset.view??""))}),p.querySelectorAll("[data-delete]").forEach(t=>{t.addEventListener("click",()=>j(t.dataset.delete??""))})}catch(e){p.innerHTML=`<p class="error">Could not load saved reports: ${a(C(e))}</p>`}}async function U(e){await d("Loading report...",async()=>{const t=await c(`/api/reports/${encodeURIComponent(e)}`);N(t)})}async function j(e){window.confirm("Delete this saved report?")&&await d("Deleting report...",async()=>{await c(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),await g(),u("Report deleted.")})}function N(e){P.clear();const t=e.markdown??"",n=e.gpuSearchContext??null,r=e.llmSummary??"";v("markdown",t),n&&v("context",JSON.stringify(n,null,2)),r&&v("llm",r),m.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Review report</p>
          <h2>${a(Z(e))}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="markdown">Copy Markdown</button>
          ${n?'<button class="ghost small" data-copy="context">Copy gpu-search context</button>':""}
          ${r?'<button class="ghost small" data-copy="llm">Copy LLM summary</button>':""}
        </div>
      </header>
      ${H(e)}
      ${D(e.findings??[])}
      ${z(n)}
      ${r?i("LLM Summary",`<div class="llm-summary">${a(r)}</div>`):""}
      ${K(t)}
    </article>
  `,m.querySelectorAll("[data-copy]").forEach(s=>{s.addEventListener("click",()=>ee(s.dataset.copy??""))})}function H(e){const t=e.generatedAtUtc??e.createdAtUtc??e.createdAt;return i("Summary",`
      <div class="meta-grid">
        ${w("Repository",e.repoPath)}
        ${w("Created",t?T(t):void 0)}
        ${w("Changed files",String(e.changedFileCount??e.files?.length??"n/a"))}
        ${w("Provider / mode",e.providerName??e.llmProvider??e.reviewMode??e.analysisMode)}
      </div>
    `)}function D(e){return e.length===0?i("Findings",'<p class="empty">No findings returned.</p>'):i("Findings",`<div class="finding-list">${e.map(q).join("")}</div>`)}function q(e){const t=_(e.severity),n=e.path??e.filePath,r=L(e);return`
    <article class="finding-card">
      <div class="row">
        <span class="badge severity-${t.toLowerCase()}">${t}</span>
        ${e.category?`<span class="badge">${a(e.category)}</span>`:""}
        ${e.ruleId?`<span class="muted mono">${a(e.ruleId)}</span>`:""}
      </div>
      <h3>${a(e.title??e.message??"Review finding")}</h3>
      ${n?`<p class="mono muted">${a(n)}${r?`:${a(r)}`:""}</p>`:""}
      ${e.description?`<p>${a(e.description)}</p>`:""}
    </article>
  `}function z(e){if(!e)return i("gpu-search Context",'<p class="empty">No gpu-search context was included in this report.</p>');if(e.wasAvailable===!1)return i("gpu-search Context",`<p class="error">gpu-search unavailable: ${a(e.unavailableReason??"No reason provided.")}</p>`);const t=e.warnings?.length?y("Global warnings",e.warnings):"",n=e.limitations?.length?y("Global limitations",e.limitations):"",r=e.files?.length?e.files.map(G).join(""):'<p class="empty">No per-file gpu-search context returned.</p>';return i("gpu-search Context",`${t}${n}<div class="context-list">${r}</div>`)}function G(e){const t=e.dependencyImpact,n=Q(t?.impactedFiles??e.impactedFiles,t?.directImporters),r=t?.warnings??e.warnings??[],s=t?.limitations??e.limitations??[],o=e.skeleton,l=typeof o=="string"?o:o?.content;return`
    <article class="context-card">
      <div class="row">
        <h3 class="mono">${a(e.filePath??e.path??"Unknown file")}</h3>
        <div class="actions compact">
          ${R(t?.confidence??e.confidence,"confidence")}
          ${R(t?.analysisMode??e.analysisMode,"mode")}
        </div>
      </div>
      ${e.error?`<p class="error">${a(e.error)}</p>`:""}
      <p class="muted">${a(t?.summary??Y(t,n))}</p>
      ${n.length?W(n):""}
      ${r.length?y("Warnings",r):""}
      ${s.length?y("Limitations",s):""}
      ${J(e.relatedResults??[])}
      ${l?`<details><summary>Skeleton preview${typeof o=="object"&&o?.language?` · ${a(o.language)}`:""}</summary><pre>${a(l)}</pre></details>`:""}
    </article>
  `}function J(e){return e.length===0?'<p class="muted">No related search results.</p>':`
    <div class="related-list">
      <h4>Related search results</h4>
      ${e.map(t=>`
            <article class="related-card">
              <div class="row">
                <strong class="mono">${a(t.file??t.filePath??"Unknown file")}${L(t)?`:${a(L(t))}`:""}</strong>
                <span class="badge">${a(t.engine??"search")}${t.score!==null&&t.score!==void 0?` · ${t.score.toFixed(2)}`:""}</span>
              </div>
              ${t.reason?`<p class="muted">${a(t.reason)}</p>`:""}
              ${t.snippet?`<pre>${a(t.snippet)}</pre>`:""}
            </article>
          `).join("")}
    </div>
  `}function K(e){return`
    <details class="raw-markdown">
      <summary>Raw Markdown</summary>
      <pre>${a(e||"No Markdown returned.")}</pre>
    </details>
  `}async function d(e,t){b(e);try{await t(),b("Ready")}catch(n){b("Error"),u(C(n),!0)}}async function c(e,t){const n=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!n.ok){const r=await n.text();throw new Error(r||`${n.status} ${n.statusText}`)}if(n.status!==204)return await n.json()}function V(e){const t=e.reportId??e.id??"";return`
    <article class="report">
      <div>
        <strong>${a(e.repoPath??"Unknown repository")}</strong>
        <span>${T(e.generatedAtUtc??e.createdAtUtc)} · ${e.changedFileCount??"n/a"} files · ${a(e.llmProvider??e.providerName??"n/a")}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${a(t)}">View</button>
        <button class="danger small" data-delete="${a(t)}">Delete</button>
      </div>
    </article>
  `}function k(e,t){return`
    <div class="result-card">
      <h3>${a(e)}</h3>
      <ul>${t.map(n=>`<li>${a(n)}</li>`).join("")}</ul>
    </div>
  `}function i(e,t){return`
    <section class="viewer-section">
      <h2>${a(e)}</h2>
      ${t}
    </section>
  `}function w(e,t){return`
    <div class="meta-item">
      <span>${a(e)}</span>
      <strong>${a(t||"n/a")}</strong>
    </div>
  `}function W(e){return`
    <div class="mini-block">
      <h4>Impacted files</h4>
      <ul>
        ${e.map(t=>{const n=t.file??t.filePath??t.path??"Unknown file",r=t.hops!==null&&t.hops!==void 0?` · ${t.hops} hop${t.hops===1?"":"s"}`:"",s=t.reason?` <span class="reason">— ${a(t.reason)} <em>(heuristic)</em></span>`:"";return`<li class="mono">${a(n)}${r}${s}</li>`}).join("")}
      </ul>
    </div>
  `}function Q(e,t){return Array.isArray(e)&&e.length>0?e.map(n=>typeof n=="string"?{file:n,hops:1}:n):(t??[]).map(n=>({file:n,hops:1}))}function y(e,t){return`
    <details>
      <summary>${a(e)} (${t.length})</summary>
      <ul>${t.map(n=>`<li>${a(n)}</li>`).join("")}</ul>
    </details>
  `}function R(e,t=""){return e?`<span class="badge ${t}">${a(e)}</span>`:""}function X(e){return`${e.status??"M"} ${e.path??e.filePath??"Unknown file"} (+${e.additions??0}/-${e.deletions??0})`}function Y(e,t){return e?`${e.totalImpacted??t.length} impacted file(s) identified.`:"No dependency impact summary returned."}function Z(e){return e.reportId??e.id??"Review report"}function L(e){return e.lineStart&&e.lineEnd?e.lineStart===e.lineEnd?String(e.lineStart):`${e.lineStart}-${e.lineEnd}`:e.lineStart?String(e.lineStart):e.line?String(e.line):""}function _(e){const t=(e??"Info").toLowerCase();return["low","medium","high","critical"].includes(t)?t[0].toUpperCase()+t.slice(1):"Info"}function v(e,t){t&&P.set(e,t)}async function ee(e){const t=P.get(e);if(!t){u("Nothing to copy.",!0);return}try{if(!navigator.clipboard)throw new Error("Clipboard API is not available.");await navigator.clipboard.writeText(t),u("Copied.")}catch(n){u(`Copy failed: ${C(n)}`,!0)}}function h(e,t){f(e).addEventListener("click",()=>{t()})}function M(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function f(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function b(e){A.textContent=e}function u(e,t=!1){$.textContent=e,$.className=t?"toast show error":"toast show",window.setTimeout(()=>{$.className="toast"},2600)}function T(e){if(!e)return"n/a";const t=new Date(e);return Number.isNaN(t.getTime())?e:t.toLocaleString()}function C(e){return e instanceof Error?e.message:String(e)}function a(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
