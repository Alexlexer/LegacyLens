(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const s of document.querySelectorAll('link[rel="modulepreload"]'))i(s);new MutationObserver(s=>{for(const r of s)if(r.type==="childList")for(const l of r.addedNodes)l.tagName==="LINK"&&l.rel==="modulepreload"&&i(l)}).observe(document,{childList:!0,subtree:!0});function a(s){const r={};return s.integrity&&(r.integrity=s.integrity),s.referrerPolicy&&(r.referrerPolicy=s.referrerPolicy),s.crossOrigin==="use-credentials"?r.credentials="include":s.crossOrigin==="anonymous"?r.credentials="omit":r.credentials="same-origin",r}function i(s){if(s.ep)return;s.ep=!0;const r=a(s);fetch(s.href,r)}})();const E=document.querySelector("#app");if(!E)throw new Error("App root was not found.");E.innerHTML=`
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
`;const S=M("repoPath"),I=M("useLlm"),m=h("output"),A=h("statusPill"),u=h("reports"),v=h("toast"),P=new Map;f("statusButton",x);f("previewButton",F);f("reviewButton",B);f("analyzeButton",O);f("refreshReportsButton",g);g();async function x(){await d("Checking gpu-search...",async()=>{const e=await c("/api/search/status");m.innerHTML=k(e.isAvailable?"gpu-search is available":"gpu-search is unavailable",[`Health: ${e.health?.status??"n/a"}`,`Backend: ${e.stats?.backend??"n/a"}`,`Device: ${e.stats?.device??"n/a"}`,`Indexed files: ${e.stats?.indexedFileCount??"n/a"}`,e.error?`Error: ${e.error}`:""].filter(Boolean))})}async function F(){await d("Loading diff preview...",async()=>{const e=await c("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:S.value})});m.innerHTML=`
      ${k(`${e.changedFileCount??0} changed file(s)`,(e.files??[]).map(Q))}
      <pre>${n(e.diff||"No diff content.")}</pre>
    `})}async function B(){await d("Generating review...",async()=>{const e=await c("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:S.value,useLlm:I.checked})});N(e),await g()})}async function O(){await d("Running .NET analysis...",async()=>{const e=await c("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:S.value,limitPerPreset:8})});m.innerHTML=k(`${e.findings?.length??0} preset finding(s)`,(e.findings??[]).map(t=>`${t.presetId??"preset"} · ${t.filePath??"unknown file"}${t.line?`:${t.line}`:""} · ${t.snippet??""}`))})}async function g(){u.innerHTML='<p class="muted">Loading saved reports...</p>';try{const e=await c("/api/reports");u.innerHTML=e.length?e.map(V).join(""):'<p class="empty">No saved reports yet. Run a review to create one.</p>',u.querySelectorAll("[data-view]").forEach(t=>{t.addEventListener("click",()=>j(t.dataset.view??""))}),u.querySelectorAll("[data-delete]").forEach(t=>{t.addEventListener("click",()=>H(t.dataset.delete??""))})}catch(e){u.innerHTML=`<p class="error">Could not load saved reports: ${n(C(e))}</p>`}}async function j(e){await d("Loading report...",async()=>{const t=await c(`/api/reports/${encodeURIComponent(e)}`);N(t)})}async function H(e){window.confirm("Delete this saved report?")&&await d("Deleting report...",async()=>{await c(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),await g(),p("Report deleted.")})}function N(e){P.clear();const t=e.markdown??"",a=e.gpuSearchContext??null,i=e.llmSummary??"";$("markdown",t),a&&$("context",JSON.stringify(a,null,2)),i&&$("llm",i),m.innerHTML=`
    <article class="viewer">
      <header class="viewer-header">
        <div>
          <p class="eyebrow">Review report</p>
          <h2>${n(Y(e))}</h2>
        </div>
        <div class="actions compact">
          <button class="ghost small" data-copy="markdown">Copy Markdown</button>
          ${a?'<button class="ghost small" data-copy="context">Copy gpu-search context</button>':""}
          ${i?'<button class="ghost small" data-copy="llm">Copy LLM summary</button>':""}
        </div>
      </header>
      ${U(e)}
      ${D(e.findings??[])}
      ${z(a)}
      ${i?o("LLM Summary",`<div class="llm-summary">${n(i)}</div>`):""}
      ${K(t)}
    </article>
  `,m.querySelectorAll("[data-copy]").forEach(s=>{s.addEventListener("click",()=>_(s.dataset.copy??""))})}function U(e){const t=e.generatedAtUtc??e.createdAtUtc??e.createdAt;return o("Summary",`
      <div class="meta-grid">
        ${w("Repository",e.repoPath)}
        ${w("Created",t?T(t):void 0)}
        ${w("Changed files",String(e.changedFileCount??e.files?.length??"n/a"))}
        ${w("Provider / mode",e.providerName??e.llmProvider??e.reviewMode??e.analysisMode)}
      </div>
    `)}function D(e){return e.length===0?o("Findings",'<p class="empty">No findings returned.</p>'):o("Findings",`<div class="finding-list">${e.map(q).join("")}</div>`)}function q(e){const t=Z(e.severity),a=e.path??e.filePath,i=L(e);return`
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
  `}function z(e){if(!e)return o("gpu-search Context",'<p class="empty">No gpu-search context was included in this report.</p>');if(e.wasAvailable===!1)return o("gpu-search Context",`<p class="error">gpu-search unavailable: ${n(e.unavailableReason??"No reason provided.")}</p>`);const t=e.warnings?.length?y("Global warnings",e.warnings):"",a=e.limitations?.length?y("Global limitations",e.limitations):"",i=e.files?.length?e.files.map(G).join(""):'<p class="empty">No per-file gpu-search context returned.</p>';return o("gpu-search Context",`${t}${a}<div class="context-list">${i}</div>`)}function G(e){const t=e.dependencyImpact,a=t?.directImporters??t?.impactedFiles??e.impactedFiles??[],i=t?.warnings??e.warnings??[],s=t?.limitations??e.limitations??[],r=e.skeleton,l=typeof r=="string"?r:r?.content;return`
    <article class="context-card">
      <div class="row">
        <h3 class="mono">${n(e.filePath??e.path??"Unknown file")}</h3>
        <div class="actions compact">
          ${R(t?.confidence??e.confidence,"confidence")}
          ${R(t?.analysisMode??e.analysisMode,"mode")}
        </div>
      </div>
      ${e.error?`<p class="error">${n(e.error)}</p>`:""}
      <p class="muted">${n(t?.summary??X(t,a))}</p>
      ${a.length?W("Impacted files",a):""}
      ${i.length?y("Warnings",i):""}
      ${s.length?y("Limitations",s):""}
      ${J(e.relatedResults??[])}
      ${l?`<details><summary>Skeleton preview${typeof r=="object"&&r?.language?` · ${n(r.language)}`:""}</summary><pre>${n(l)}</pre></details>`:""}
    </article>
  `}function J(e){return e.length===0?'<p class="muted">No related search results.</p>':`
    <div class="related-list">
      <h4>Related search results</h4>
      ${e.map(t=>`
            <article class="related-card">
              <div class="row">
                <strong class="mono">${n(t.file??t.filePath??"Unknown file")}${L(t)?`:${n(L(t))}`:""}</strong>
                <span class="badge">${n(t.engine??"search")}${t.score!==null&&t.score!==void 0?` · ${t.score.toFixed(2)}`:""}</span>
              </div>
              ${t.reason?`<p class="muted">${n(t.reason)}</p>`:""}
              ${t.snippet?`<pre>${n(t.snippet)}</pre>`:""}
            </article>
          `).join("")}
    </div>
  `}function K(e){return`
    <details class="raw-markdown">
      <summary>Raw Markdown</summary>
      <pre>${n(e||"No Markdown returned.")}</pre>
    </details>
  `}async function d(e,t){b(e);try{await t(),b("Ready")}catch(a){b("Error"),p(C(a),!0)}}async function c(e,t){const a=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!a.ok){const i=await a.text();throw new Error(i||`${a.status} ${a.statusText}`)}if(a.status!==204)return await a.json()}function V(e){const t=e.reportId??e.id??"";return`
    <article class="report">
      <div>
        <strong>${n(e.repoPath??"Unknown repository")}</strong>
        <span>${T(e.generatedAtUtc??e.createdAtUtc)} · ${e.changedFileCount??"n/a"} files · ${n(e.llmProvider??e.providerName??"n/a")}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${n(t)}">View</button>
        <button class="danger small" data-delete="${n(t)}">Delete</button>
      </div>
    </article>
  `}function k(e,t){return`
    <div class="result-card">
      <h3>${n(e)}</h3>
      <ul>${t.map(a=>`<li>${n(a)}</li>`).join("")}</ul>
    </div>
  `}function o(e,t){return`
    <section class="viewer-section">
      <h2>${n(e)}</h2>
      ${t}
    </section>
  `}function w(e,t){return`
    <div class="meta-item">
      <span>${n(e)}</span>
      <strong>${n(t||"n/a")}</strong>
    </div>
  `}function W(e,t){return`
    <div class="mini-block">
      <h4>${n(e)}</h4>
      <ul>${t.map(a=>`<li class="mono">${n(a)}</li>`).join("")}</ul>
    </div>
  `}function y(e,t){return`
    <details>
      <summary>${n(e)} (${t.length})</summary>
      <ul>${t.map(a=>`<li>${n(a)}</li>`).join("")}</ul>
    </details>
  `}function R(e,t=""){return e?`<span class="badge ${t}">${n(e)}</span>`:""}function Q(e){return`${e.status??"M"} ${e.path??e.filePath??"Unknown file"} (+${e.additions??0}/-${e.deletions??0})`}function X(e,t){return e?`${e.totalImpacted??t.length} impacted file(s) identified.`:"No dependency impact summary returned."}function Y(e){return e.reportId??e.id??"Review report"}function L(e){return e.lineStart&&e.lineEnd?e.lineStart===e.lineEnd?String(e.lineStart):`${e.lineStart}-${e.lineEnd}`:e.lineStart?String(e.lineStart):e.line?String(e.line):""}function Z(e){const t=(e??"Info").toLowerCase();return["low","medium","high","critical"].includes(t)?t[0].toUpperCase()+t.slice(1):"Info"}function $(e,t){t&&P.set(e,t)}async function _(e){const t=P.get(e);if(!t){p("Nothing to copy.",!0);return}try{if(!navigator.clipboard)throw new Error("Clipboard API is not available.");await navigator.clipboard.writeText(t),p("Copied.")}catch(a){p(`Copy failed: ${C(a)}`,!0)}}function f(e,t){h(e).addEventListener("click",()=>{t()})}function M(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function h(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function b(e){A.textContent=e}function p(e,t=!1){v.textContent=e,v.className=t?"toast show error":"toast show",window.setTimeout(()=>{v.className="toast"},2600)}function T(e){if(!e)return"n/a";const t=new Date(e);return Number.isNaN(t.getTime())?e:t.toLocaleString()}function C(e){return e instanceof Error?e.message:String(e)}function n(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
