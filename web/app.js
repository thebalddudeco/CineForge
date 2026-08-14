const state = {
  project: null,
  activeBranch: "angles",
  selected: new Set(),
  references: {},
  models: null,
  runtime: null,
  runtimeSamples: [],
  generation: null,
};

const $ = (selector, root = document) => root.querySelector(selector);
const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];

function toast(message, error = false) {
  const node = $("#toast");
  node.textContent = message;
  node.className = `toast show${error ? " error" : ""}`;
  clearTimeout(toast.timer);
  toast.timer = setTimeout(() => node.className = "toast", 3600);
}

async function api(path, options = {}) {
  const response = await fetch(path, {
    ...options,
    headers: { "Content-Type": "application/json", ...(options.headers || {}) },
  });
  const result = await response.json();
  if (!response.ok) throw new Error(result.error || `Request failed (${response.status})`);
  return result;
}

function formatClock(seconds) {
  if (!Number.isFinite(seconds) || seconds < 0) return "—";
  const whole = Math.max(0, Math.round(seconds));
  const minutes = Math.floor(whole / 60);
  return `${String(minutes).padStart(2, "0")}:${String(whole % 60).padStart(2, "0")}`;
}

function deviceName(runtime) {
  return String(runtime?.device || "Local GPU")
    .replace(/^cuda:\d+\s*/i, "")
    .replace(/\s*:\s*cudaMallocAsync\s*$/i, "");
}

function drawLine(ctx, points, color, width = 1) {
  if (!points.length) return;
  ctx.beginPath();
  points.forEach(([x, y], index) => index ? ctx.lineTo(x, y) : ctx.moveTo(x, y));
  ctx.strokeStyle = color;
  ctx.lineWidth = width;
  ctx.stroke();
}

function drawRuntimeCanvases(timestamp = 0) {
  const compact = $("#runtimeSpark");
  const history = $("#runtimeHistory");
  const samples = state.runtimeSamples.length ? state.runtimeSamples : [{ used: 0, free: 0, online: 0, delta: 0 }];
  const acid = "#d7ff45", ink = "#e9e5dc", muted = "#918f89", line = "#292925", black = "#090908";

  const paint = (canvas, expanded) => {
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    const { width, height } = canvas;
    ctx.clearRect(0, 0, width, height);
    ctx.fillStyle = black;
    ctx.fillRect(0, 0, width, height);
    ctx.strokeStyle = line;
    ctx.lineWidth = 1;
    const gridX = expanded ? 28 : 18;
    const gridY = expanded ? 28 : 16;
    for (let x = 0; x <= width; x += gridX) { ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, height); ctx.stroke(); }
    for (let y = 0; y <= height; y += gridY) { ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(width, y); ctx.stroke(); }

    const count = expanded ? 54 : 32;
    const padded = Array(Math.max(0, count - samples.length)).fill(samples[0]).concat(samples.slice(-count));
    const xFor = index => index * (width - 2) / Math.max(1, padded.length - 1) + 1;
    const tracks = expanded
      ? [
          { key: "used", color: acid, base: height * .19, span: height * .14 },
          { key: "free", color: muted, base: height * .43, span: height * .12 },
          { key: "delta", color: ink, base: height * .67, span: height * .12 },
          { key: "online", color: muted, base: height * .88, span: height * .08 },
        ]
      : [{ key: "used", color: acid, base: height * .72, span: height * .52 }];
    tracks.forEach(track => {
      const pts = padded.map((sample, index) => {
        const value = Math.max(0, Math.min(100, Number(sample[track.key] || 0)));
        return [xFor(index), track.base - (value / 100) * track.span];
      });
      drawLine(ctx, pts, track.color, expanded && track.key === "used" ? 1.4 : 1);
    });
    const scanX = ((timestamp / 14) % width);
    ctx.strokeStyle = acid;
    ctx.globalAlpha = .75;
    ctx.beginPath(); ctx.moveTo(scanX, 0); ctx.lineTo(scanX, height); ctx.stroke();
    ctx.globalAlpha = 1;
  };
  paint(compact, false);
  paint(history, true);
  requestAnimationFrame(drawRuntimeCanvases);
}

async function loadRuntime() {
  const consoleNode = $("#runtimeConsole");
  try {
    const health = await api("/api/health");
    const runtime = health.runtime;
    state.runtime = runtime;
    const total = Number(runtime.vram_total_gb || 0);
    const free = Number(runtime.vram_free_gb || 0);
    const usedGb = Math.max(0, total - free);
    const used = total ? usedGb / total * 100 : 0;
    const previous = state.runtimeSamples.at(-1);
    state.runtimeSamples.push({ used, free: total ? free / total * 100 : 0, online: runtime.online ? 100 : 0, delta: previous ? Math.min(100, Math.abs(used - previous.used) * 12) : 0 });
    if (state.runtimeSamples.length > 90) state.runtimeSamples.shift();
    consoleNode.className = `runtime-console ${runtime.online ? "online" : "offline"}`;
    $("#runtimeState").textContent = runtime.online ? "CONNECTED / ONLINE" : "RUNTIME OFFLINE";
    $("#runtimeDevice").textContent = runtime.online ? deviceName(runtime) : "CINEFORGE ENGINE";
    $("#runtimeSummary").textContent = runtime.online ? `${usedGb.toFixed(1)} / ${total.toFixed(1)} GB VRAM · ${runtime.engine} ${runtime.engine_version}` : "No compatible GPU runtime response";
    $("#runtimeLinkState").textContent = runtime.online ? "CONNECTED / 01" : "DISCONNECTED / 00";
    $("#runtimeGpu").textContent = runtime.online ? deviceName(runtime).replace("NVIDIA GeForce ", "") : "—";
    $("#runtimeVram").textContent = runtime.online ? `${Math.round(used)}%` : "—";
    $("#runtimeEngine").textContent = /cudaMallocAsync/i.test(runtime.device || "") ? "CUDA / ASYNC" : runtime.online ? "CUDA" : "—";
    $("#runtimeBuild").textContent = runtime.engine_version || "—";
  } catch (error) {
    state.runtime = { online: false };
    const previous = state.runtimeSamples.at(-1) || { used: 0 };
    state.runtimeSamples.push({ used: previous.used, free: 0, online: 0, delta: 0 });
    consoleNode.className = "runtime-console offline";
    $("#runtimeState").textContent = "RUNTIME CHECK FAILED";
    $("#runtimeDevice").textContent = "CINEFORGE ENGINE";
    $("#runtimeSummary").textContent = error.message;
  }
}

function matrixHash(column, row) {
  const value = Math.sin((column + 1) * 12.9898 + (row + 1) * 78.233) * 43758.5453;
  return value - Math.floor(value);
}

function shuffledCells(total) {
  return Array.from({ length: total }, (_, index) => index).sort((a, b) => matrixHash(a, 19) - matrixHash(b, 19));
}

const matrixDeck = shuffledCells(38 * 7);

function drawGenerationMatrix(timestamp = 0) {
  const canvas = $("#generationMatrix");
  if (!canvas) return;
  const ctx = canvas.getContext("2d");
  const { width, height } = canvas;
  ctx.clearRect(0, 0, width, height);
  const cols = 38, rows = 7;
  const pitchX = width / (cols + 1), pitchY = height / (rows + 1);
  const stateIndex = Math.floor(timestamp / 420) % 12;
  const green = new Set(matrixDeck.slice(stateIndex * 6, stateIndex * 6 + 6));
  for (let row = 0; row < rows; row += 1) {
    for (let col = 0; col < cols; col += 1) {
      const cell = row * cols + col;
      const phase = matrixHash(col, row) * Math.PI * 2;
      const breath = Math.pow(.5 - .5 * Math.cos(timestamp / 5000 * Math.PI * 2 + phase), 1.35);
      const isGreen = green.has(cell);
      ctx.fillStyle = isGreen ? `rgba(215,255,69,${breath})` : `rgba(233,229,220,${breath})`;
      const size = Math.max(3, Math.min(7, pitchX * .52));
      ctx.fillRect((col + 1) * pitchX - size / 2, (row + 1) * pitchY - size / 2, size, size);
    }
  }
  requestAnimationFrame(drawGenerationMatrix);
}

function updateGenerationUi() {
  const job = state.generation;
  if (!job) return;
  const elapsed = (Date.now() - job.startedAt) / 1000;
  const percent = job.max > 0 ? Math.max(0, Math.min(100, job.value / job.max * 100)) : job.status === "complete" ? 100 : 0;
  const eta = percent > 1 && percent < 100 ? elapsed * (100 - percent) / percent : null;
  $("#generationTitle").textContent = job.status === "complete" ? "COMPLETE" : job.status === "error" ? "GENERATION ERROR" : "GENERATING";
  $("#generationPhase").textContent = job.phase;
  $("#generationPercent").textContent = `${String(Math.round(percent)).padStart(2, "0")}%`;
  $("#generationDetail").textContent = job.detail;
  $("#generationStep").textContent = job.max ? `STEP ${job.value} / ${job.max}` : "STEP — / —";
  $("#generationElapsed").textContent = formatClock(elapsed);
  $("#generationEta").textContent = job.status === "complete" ? "00:00" : formatClock(eta);
  $("#generationRuntime").textContent = state.runtime?.online ? "ONLINE / NATIVE" : "AWAITING GPU";
  $$("#generationSegments i").forEach((segment, index, all) => segment.classList.toggle("active", index < Math.round(percent / 100 * all.length)));
  $$("#generationSignal i").forEach((segment, index) => segment.classList.toggle("active", state.runtime?.online && index < (job.status === "running" ? 7 : 5)));
  const windowNode = $("#generationWindow");
  windowNode.classList.toggle("complete", job.status === "complete");
  windowNode.classList.toggle("error", job.status === "error");
}

function startGeneration(result, options) {
  state.generation = {
    promptId: result.prompt_id,
    clientId: result.client_id,
    value: 0,
    max: Number(result.steps || 0),
    startedAt: Date.now(),
    status: "running",
    phase: "QUEUEING LOCAL JOB",
    detail: options.detail,
    kind: options.kind,
  };
  $("#generationWindow").classList.remove("hidden", "complete", "error");
  updateGenerationUi();
}

function finishGeneration(status, message = "") {
  const job = state.generation;
  if (!job) return;
  job.status = status;
  if (status === "complete") {
    job.value = job.max || 1;
    job.max = job.max || 1;
    job.phase = "OUTPUT SAVED / SIGNAL LOCK";
  } else {
    job.phase = "RUNTIME REPORTED AN ERROR";
    if (message) job.detail = message;
  }
  updateGenerationUi();
  if (status === "complete") setTimeout(() => {
    if (state.generation === job) $("#generationWindow").classList.add("hidden");
  }, 5000);
}


async function loadModels() {
  const strip = $("#modelStrip");
  strip.innerHTML = '<div class="skeleton"></div><div class="skeleton"></div><div class="skeleton"></div>';
  try {
    state.models = await api("/api/models");
    strip.innerHTML = state.models.families.map(family => `
      <div class="model-chip">
        <div class="model-top"><strong>${escapeHtml(family.label)}</strong><i title="${family.runnable ? "Runnable" : "Discovered"}"></i></div>
        <small>${escapeHtml(family.capability)} · ${family.asset_count} asset${family.asset_count === 1 ? "" : "s"} · ${family.size_gb} GB</small>
      </div>`).join("") || '<div class="empty-motion">No compatible model assets found in configured roots.</div>';
  } catch (error) {
    strip.innerHTML = `<div class="empty-motion">${escapeHtml(error.message)}</div>`;
  }
}

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>'"]/g, char => ({"&":"&amp;","<":"&lt;",">":"&gt;","'":"&#39;",'"':"&quot;"})[char]);
}

function formPayload() {
  return Object.fromEntries(new FormData($("#briefForm")).entries());
}

function allShots() {
  if (!state.project) return [];
  return Object.values(state.project.branches).flat();
}

function findShot(id) {
  return allShots().find(shot => shot.id === id);
}

function renderShots() {
  const shots = state.project?.branches[state.activeBranch] || [];
  $("#shotGrid").innerHTML = shots.map(shot => {
    const selected = state.selected.has(shot.id);
    const output = shot.still_output;
    const media = output ? `<img src="${escapeHtml(output.url)}" alt="Generated ${escapeHtml(shot.title)}">` : '<span>16:9 PROOF FRAME</span>';
    return `
      <article class="shot-card${selected ? " selected" : ""}" data-shot="${shot.id}">
        <div class="shot-preview">${media}</div>
        <div class="shot-body">
          <span class="shot-index">${shot.branch.toUpperCase()} / 0${shot.index + 1}</span>
          <h3>${escapeHtml(shot.title)}</h3>
          <p title="${escapeHtml(shot.prompt)}">${escapeHtml(shot.prompt)}</p>
          <div class="shot-actions">
            <button class="render-still" data-id="${shot.id}" ${shot.still_job_id && !shot.still_output ? "disabled" : ""}>${shot.still_job_id && !shot.still_output ? "Rendering…" : output ? "Rerender" : "Render proof"}</button>
            <button class="${selected ? "approve" : ""} toggle-shot" data-id="${shot.id}">${selected ? "Selected ✓" : "Select"}</button>
          </div>
        </div>
      </article>`;
  }).join("");
  $("#selectedCount").textContent = state.selected.size;
}

function renderMotion() {
  const selectedShots = allShots().filter(shot => state.selected.has(shot.id));
  const list = $("#motionList");
  if (!selectedShots.length) {
    list.innerHTML = '<div class="empty-motion">Select one or more shots to prepare motion prompts.</div>';
    return;
  }
  list.innerHTML = selectedShots.map((shot, index) => `
    <article class="motion-card" data-motion="${shot.id}">
      <div class="motion-number">0${index + 1}</div>
      <div class="motion-copy"><strong>${escapeHtml(shot.title)}</strong><p title="${escapeHtml(shot.motion_prompt)}">${escapeHtml(shot.motion_prompt)}</p></div>
      <div class="motion-controls">
        <select data-quality><option value="proof">Proof · 25 frames</option><option value="final">Final · 20 steps</option></select>
        <button data-video="${shot.id}" ${!shot.still_output || (shot.video_job_id && !shot.video_output) ? "disabled" : ""}>${!shot.still_output ? "Render still first" : shot.video_job_id && !shot.video_output ? "Animating…" : shot.video_output ? "Animate again" : "Animate with Wan 2.2"}</button>
      </div>
      ${shot.video_output ? `<video src="${escapeHtml(shot.video_output.url)}" controls loop></video>` : ""}
    </article>`).join("");
}

function updateProjectSelection() {
  if (!state.project) return;
  state.project.selection = [...state.selected];
  allShots().forEach(shot => shot.approved = state.selected.has(shot.id));
  $("#motion").classList.toggle("hidden", !state.selected.size);
  renderMotion();
}

async function buildPlan(event) {
  event.preventDefault();
  const button = $("#briefForm .primary-button");
  button.disabled = true;
  button.querySelector("span").textContent = "Building shot factory…";
  try {
    state.project = await api("/api/plan", { method: "POST", body: JSON.stringify(formPayload()) });
    state.selected.clear();
    $("#factory").classList.remove("hidden");
    $("#saveState").textContent = `Project ${state.project.project_id}`;
    $("#factoryTitle").textContent = `${state.project.title}: fifteen candidates.`;
    renderShots();
    updateProjectSelection();
    $("#factory").scrollIntoView({ behavior: "smooth", block: "start" });
    toast("Shot factory created: 5 angles, 5 inserts, 5 story beats.");
  } catch (error) {
    toast(error.message, true);
  } finally {
    button.disabled = false;
    button.querySelector("span").textContent = "Build 15-shot factory";
  }
}

async function uploadReference(input) {
  const file = input.files[0];
  if (!file) return;
  const role = input.dataset.role;
  const label = input.closest(".reference-slot");
  const reader = new FileReader();
  reader.onload = async () => {
    const preview = document.createElement("img");
    preview.src = reader.result;
    label.querySelector("img")?.remove();
    label.appendChild(preview);
    label.classList.add("loaded");
    try {
      const result = await api("/api/upload", { method: "POST", body: JSON.stringify({ name: `${role}-${file.name}`, data: reader.result }) });
      state.references[role] = result.asset.path;
      $("#referenceCount").textContent = `${Object.keys(state.references).length} / 5`;
      toast(`${role} reference locked.`);
    } catch (error) {
      label.classList.remove("loaded");
      toast(error.message, true);
    }
  };
  reader.readAsDataURL(file);
}

function chosenReference() {
  return state.references.identity || state.references.geography || state.references.look || null;
}

function availableStillModel() {
  const adapters = state.models?.adapters || [];
  return adapters.find(item => item.kind === "still" && item.available && item.native_pack && !item.diagnostic)?.id || "";
}

function availableVideoModel() {
  const adapters = state.models?.adapters || [];
  return adapters.find(item => item.kind === "video" && item.available && item.native_pack && !item.diagnostic)?.id || "";
}

async function renderStill(id, button) {
  const shot = findShot(id);
  if (!shot) return;
  button.disabled = true;
  button.textContent = "Queueing…";
  try {
    const modelId = availableStillModel();
    if (!modelId) throw new Error("No standalone still-image model pack is installed. Raw ComfyUI checkpoints must be converted first.");
    const result = await api("/api/render/still", { method: "POST", body: JSON.stringify({
      prompt: shot.prompt,
      model_id: modelId,
      reference_image: chosenReference(),
      width: 768, height: 432, quality: "proof",
    }) });
    shot.still_job_id = result.prompt_id;
    startGeneration(result, { kind: "still", detail: `${shot.title.toUpperCase()} / PROOF FRAME` });
    renderShots();
    toast(`${shot.title} queued on the local GPU.`);
    pollJob(result.prompt_id, output => {
      shot.still_output = output;
      renderShots();
      renderMotion();
      saveProject(true);
    }, error => {
      shot.still_job_id = null;
      renderShots();
      toast(error.message, true);
    });
  } catch (error) {
    shot.still_job_id = null;
    renderShots();
    toast(error.message, true);
  }
}

async function renderVideo(id, button) {
  const shot = findShot(id);
  if (!shot?.still_output) return;
  button.disabled = true;
  button.textContent = "Queueing…";
  const quality = button.closest(".motion-controls").querySelector("[data-quality]").value;
  const duration = Number(state.project?.brief?.duration || 5);
  const length = quality === "proof" ? 17 : Math.min(241, Math.max(17, duration * 25));
  try {
    const modelId = availableVideoModel();
    if (!modelId) throw new Error("No standalone video model pack is installed. The existing Wan files require native-pack conversion.");
    const result = await api("/api/render/video", { method: "POST", body: JSON.stringify({
      image: shot.still_output,
      prompt: shot.motion_prompt, width: 768, height: 432, length, quality, model_id: modelId,
    }) });
    shot.video_job_id = result.prompt_id;
    startGeneration(result, { kind: "video", detail: `${shot.title.toUpperCase()} / MOTION OUTPUT` });
    renderMotion();
    toast(`${shot.title} motion queued on the native engine.`);
    pollJob(result.prompt_id, output => {
      shot.video_output = output;
      renderMotion();
      saveProject(true);
    }, error => {
      shot.video_job_id = null;
      renderMotion();
      toast(error.message, true);
    });
  } catch (error) {
    shot.video_job_id = null;
    renderMotion();
    toast(error.message, true);
  }
}

async function pollJob(promptId, onComplete, onError, attempts = 0) {
  try {
    const job = await api(`/api/jobs/${encodeURIComponent(promptId)}`);
    if (state.generation?.promptId === promptId) {
      if (job.status === "queued") state.generation.phase = `QUEUED / POSITION ${job.queue_position || 1}`;
      if (job.status === "running") {
        state.generation.phase = job.phase || "NATIVE MODEL INFERENCE";
        state.generation.value = Number(job.value || 0);
        state.generation.max = Number(job.max || state.generation.max || 0);
      }
      updateGenerationUi();
    }
    if (job.status === "complete" && job.outputs.length) {
      finishGeneration("complete");
      onComplete(job.outputs[0]);
      toast("Local generation complete.");
      return;
    }
    if (job.status === "error") {
      const message = job.error || "The native local generation engine reported an error.";
      finishGeneration("error", message);
      throw new Error(message);
    }
    if (attempts > 1200) throw new Error("Generation polling timed out.");
    setTimeout(() => pollJob(promptId, onComplete, onError, attempts + 1), 2000);
  } catch (error) {
    if (onError) onError(error); else toast(error.message, true);
  }
}

async function saveProject(silent = false) {
  if (!state.project) return;
  updateProjectSelection();
  try {
    await api("/api/projects/save", { method: "POST", body: JSON.stringify(state.project) });
    $("#saveState").textContent = "Saved locally";
    if (!silent) toast("Project saved locally.");
  } catch (error) {
    if (!silent) toast(error.message, true);
  }
}

$("#briefForm").addEventListener("submit", buildPlan);
$("#refreshModels").addEventListener("click", () => { loadRuntime(); loadModels(); });
$("#runtimeConsole").addEventListener("click", () => {
  const consoleNode = $("#runtimeConsole");
  const popover = $("#runtimePopover");
  const willOpen = popover.classList.contains("hidden");
  popover.classList.toggle("hidden", !willOpen);
  consoleNode.setAttribute("aria-expanded", String(willOpen));
});
document.addEventListener("click", event => {
  if (!event.target.closest(".runtime-dock")) {
    $("#runtimePopover").classList.add("hidden");
    $("#runtimeConsole").setAttribute("aria-expanded", "false");
  }
});
$("#exitApp").addEventListener("click", async () => {
  try {
    await api("/api/shutdown", { method: "POST", body: "{}" });
    document.body.innerHTML = '<main class="stopped-screen"><div><p class="eyebrow">CINEFORGE LOCAL</p><h1>Studio closed.</h1><p>You can close this tab or launch CineForge again from the Start Menu.</p></div></main>';
  } catch (error) {
    toast(error.message, true);
  }
});
$("#saveProject").addEventListener("click", () => saveProject());
$$('#referenceGrid input[type="file"]').forEach(input => input.addEventListener("change", () => uploadReference(input)));

$("#factory").addEventListener("click", event => {
  const tab = event.target.closest(".branch-tab");
  if (tab) {
    state.activeBranch = tab.dataset.branch;
    $$(".branch-tab").forEach(item => item.classList.toggle("active", item === tab));
    renderShots();
    return;
  }
  const toggle = event.target.closest(".toggle-shot");
  if (toggle) {
    const id = toggle.dataset.id;
    state.selected.has(id) ? state.selected.delete(id) : state.selected.add(id);
    renderShots();
    updateProjectSelection();
    return;
  }
  const render = event.target.closest(".render-still");
  if (render) renderStill(render.dataset.id, render);
});

$("#motion").addEventListener("click", event => {
  const button = event.target.closest("[data-video]");
  if (button) renderVideo(button.dataset.video, button);
});

$("#generationSegments").innerHTML = "<i></i>".repeat(42);
$("#generationSignal").innerHTML = "<i></i>".repeat(8);
drawRuntimeCanvases();
drawGenerationMatrix();
setInterval(() => { if (state.generation) updateGenerationUi(); }, 250);
setInterval(loadRuntime, 3000);
loadRuntime();
loadModels();
