(function(){const t=document.createElement("link").relList;if(t&&t.supports&&t.supports("modulepreload"))return;for(const n of document.querySelectorAll('link[rel="modulepreload"]'))d(n);new MutationObserver(n=>{for(const r of n)if(r.type==="childList")for(const v of r.addedNodes)v.tagName==="LINK"&&v.rel==="modulepreload"&&d(v)}).observe(document,{childList:!0,subtree:!0});function a(n){const r={};return n.integrity&&(r.integrity=n.integrity),n.referrerPolicy&&(r.referrerPolicy=n.referrerPolicy),n.crossOrigin==="use-credentials"?r.credentials="include":n.crossOrigin==="anonymous"?r.credentials="omit":r.credentials="same-origin",r}function d(n){if(n.ep)return;n.ep=!0;const r=a(n);fetch(n.href,r)}})();const y=document.querySelector("#app");if(!y)throw new Error("App root was not found.");y.innerHTML=`
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
        <h2>Output</h2>
        <span id="statusPill" class="pill">Ready</span>
      </div>
      <div id="output" class="output muted">Run a preview, review, or analysis to see results.</div>
    </section>
  </main>
`;const w=g("repoPath"),$=g("useLlm"),o=h("output"),L=h("statusPill"),u=h("reports");l("statusButton",b);l("previewButton",P);l("reviewButton",E);l("analyzeButton",T);l("refreshReportsButton",p);p();async function b(){await c("Checking gpu-search...",async()=>{const e=await i("/api/search/status");o.innerHTML=f(e.isAvailable?"gpu-search is available":"gpu-search is unavailable",[`Health: ${e.health?.status??"n/a"}`,`Backend: ${e.stats?.backend??"n/a"}`,`Device: ${e.stats?.device??"n/a"}`,`Indexed files: ${e.stats?.indexedFileCount??"n/a"}`,e.error?`Error: ${e.error}`:""].filter(Boolean))})}async function P(){await c("Loading diff preview...",async()=>{const e=await i("/api/review/diff/preview",{method:"POST",body:JSON.stringify({repoPath:w.value})});o.innerHTML=`
      ${f(`${e.changedFileCount} changed file(s)`,e.files.map(I))}
      <pre>${s(e.diff||"No diff content.")}</pre>
    `})}async function E(){await c("Running review...",async()=>{const e=await i("/api/review/diff",{method:"POST",body:JSON.stringify({repoPath:w.value,useLlm:$.checked})});o.innerHTML=`
      ${f(`Saved report ${e.reportId}`,[`Repository: ${e.repoPath}`,`Files: ${e.changedFileCount}`,`Provider: ${e.llmProvider}`])}
      <pre>${s(e.markdown)}</pre>
    `,await p()})}async function T(){await c("Running .NET analysis...",async()=>{const e=await i("/api/dotnet/analyze",{method:"POST",body:JSON.stringify({repoPath:w.value,limitPerPreset:8})});o.innerHTML=f(`${e.findings.length} preset finding(s)`,e.findings.map(t=>`${t.presetId} · ${t.filePath}${t.line?`:${t.line}`:""} · ${t.snippet}`))})}async function p(){try{const e=await i("/api/reports");u.innerHTML=e.length?e.map(B).join(""):'<p class="muted">No saved reports yet.</p>',u.querySelectorAll("[data-view]").forEach(t=>{t.addEventListener("click",()=>R(t.dataset.view??""))}),u.querySelectorAll("[data-delete]").forEach(t=>{t.addEventListener("click",()=>S(t.dataset.delete??""))})}catch(e){u.innerHTML=`<p class="error">${s(String(e))}</p>`}}async function R(e){await c("Loading report...",async()=>{const t=await i(`/api/reports/${encodeURIComponent(e)}`);o.innerHTML=`<pre>${s(t.markdown)}</pre>`})}async function S(e){await c("Deleting report...",async()=>{await i(`/api/reports/${encodeURIComponent(e)}`,{method:"DELETE"}),await p(),o.innerHTML='<p class="muted">Report deleted.</p>'})}async function c(e,t){m(e);try{await t(),m("Ready")}catch(a){m("Error"),o.innerHTML=`<p class="error">${s(String(a))}</p>`}}async function i(e,t){const a=await fetch(e,{headers:{"Content-Type":"application/json",...t?.headers},...t});if(!a.ok){const d=await a.text();throw new Error(d||`${a.status} ${a.statusText}`)}if(a.status!==204)return await a.json()}function B(e){return`
    <article class="report">
      <div>
        <strong>${s(e.repoPath)}</strong>
        <span>${new Date(e.generatedAtUtc).toLocaleString()} · ${e.changedFileCount} files · ${e.llmProvider}</span>
      </div>
      <div class="actions compact">
        <button class="ghost small" data-view="${s(e.reportId)}">View</button>
        <button class="danger small" data-delete="${s(e.reportId)}">Delete</button>
      </div>
    </article>
  `}function f(e,t){return`
    <div class="result-card">
      <h3>${s(e)}</h3>
      <ul>${t.map(a=>`<li>${s(a)}</li>`).join("")}</ul>
    </div>
  `}function I(e){return`${e.status} ${e.path} (+${e.additions}/-${e.deletions})`}function l(e,t){h(e).addEventListener("click",()=>{t()})}function g(e){const t=document.getElementById(e);if(!(t instanceof HTMLInputElement))throw new Error(`${e} input was not found.`);return t}function h(e){const t=document.getElementById(e);if(!t)throw new Error(`${e} element was not found.`);return t}function m(e){L.textContent=e}function s(e){return e.replaceAll("&","&amp;").replaceAll("<","&lt;").replaceAll(">","&gt;").replaceAll('"',"&quot;").replaceAll("'","&#039;")}
