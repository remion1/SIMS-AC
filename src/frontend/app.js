"use strict";

// ─────────────────────────────────────────────────────────────────────────────
// SIMS Web-Frontend — single-page client for the SIMS REST API.
// All calls go to the same origin under /api (nginx proxies them to the backend).
// ─────────────────────────────────────────────────────────────────────────────

const API = "/api";
const SEVERITIES = ["Low", "Medium", "High", "Critical"];
const STATUSES = ["Open", "InProgress", "Escalated", "Closed"];
const ROLES = ["User", "Admin"];

const state = { user: null };

// ───────────────────────────── helpers ─────────────────────────────
const $ = (sel, root = document) => root.querySelector(sel);
const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];

function esc(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
}

function fmtDate(iso) {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return esc(iso);
  return d.toLocaleString("de-AT", { dateStyle: "short", timeStyle: "short" });
}

// Enum values may arrive as strings ("High") or numbers (2) depending on server config.
function sevName(v) { return typeof v === "number" ? SEVERITIES[v] ?? v : v; }
function stName(v) { return typeof v === "number" ? STATUSES[v] ?? v : v; }
function roleName(v) { return typeof v === "number" ? ROLES[v] ?? v : v; }

let toastTimer;
function toast(message, kind = "ok") {
  const el = $("#toast");
  el.textContent = message;
  el.className = `toast ${kind}`;
  el.hidden = false;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => (el.hidden = true), 3200);
}

async function api(path, { method = "GET", body } = {}) {
  const res = await fetch(API + path, {
    method,
    headers: body ? { "Content-Type": "application/json" } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  });

  let data = null;
  const text = await res.text();
  if (text) { try { data = JSON.parse(text); } catch { data = text; } }

  if (!res.ok) {
    const msg = (data && (data.message || data.Message)) ||
      (data && typeof data === "object" ? firstValidationError(data) : null) ||
      `HTTP ${res.status}`;
    throw new Error(msg);
  }
  return data;
}

function firstValidationError(obj) {
  if (obj.errors && typeof obj.errors === "object") {
    const first = Object.values(obj.errors)[0];
    if (Array.isArray(first)) return first[0];
  }
  return null;
}

// ───────────────────────────── auth ─────────────────────────────
async function doLogin(evt) {
  evt.preventDefault();
  const err = $("#login-error");
  err.hidden = true;
  try {
    const user = await api("/auth/login", {
      method: "POST",
      body: { username: $("#login-username").value.trim(), password: $("#login-password").value },
    });
    state.user = user;
    try { sessionStorage.setItem("sims_user", JSON.stringify(user)); } catch { /* ignore */ }
    enterApp();
  } catch (e) {
    err.textContent = e.message;
    err.hidden = false;
  }
}

function logout() {
  state.user = null;
  try { sessionStorage.removeItem("sims_user"); } catch { /* ignore */ }
  $("#app-view").hidden = true;
  $("#login-view").hidden = false;
  $("#login-form").reset();
}

function enterApp() {
  const u = state.user;
  const isAdmin = roleName(u.role) === "Admin";
  $("#login-view").hidden = true;
  $("#app-view").hidden = false;
  $("#current-user").innerHTML = `<b>${esc(u.username)}</b> · ${esc(roleName(u.role))}`;
  $$(".admin-only").forEach((el) => (el.style.display = isAdmin ? "" : "none"));
  switchTab("incidents");
}

// ───────────────────────────── tabs ─────────────────────────────
function switchTab(name) {
  $$(".tab").forEach((t) => t.classList.toggle("active", t.dataset.tab === name));
  $$(".tab-panel").forEach((p) => (p.hidden = p.id !== `tab-${name}`));
  if (name === "incidents") loadIncidents();
  if (name === "logs") loadLogs();
  if (name === "users") loadUsers();
}

// ───────────────────────────── incidents ─────────────────────────────
async function loadIncidents() {
  const status = $("#filter-status").value;
  const severity = $("#filter-severity").value;
  const qs = new URLSearchParams();
  if (status) qs.set("status", status);
  if (severity) qs.set("severity", severity);
  const query = qs.toString() ? `?${qs}` : "";

  try {
    const items = await api(`/incidents${query}`);
    const tbody = $("#incident-rows");
    tbody.innerHTML = "";
    $("#incident-empty").hidden = items.length > 0;

    for (const i of items) {
      const sev = sevName(i.severity), st = stName(i.status);
      const closed = st === "Closed";
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${i.id}</td>
        <td title="${esc(i.description)}">${esc(i.title)}</td>
        <td>${esc(i.affectedSystem)}</td>
        <td>${i.cveNumber ? esc(i.cveNumber) : "—"}</td>
        <td><span class="badge sev-${esc(sev)}">${esc(sev)}</span></td>
        <td><span class="badge st-${esc(st)}">${esc(st)}</span></td>
        <td>${i.assignedTo ? esc(i.assignedTo) : "—"}</td>
        <td>${fmtDate(i.timestamp)}</td>
        <td><div class="row-actions">
          <button class="btn btn-xs" data-act="edit" data-id="${i.id}">Bearbeiten</button>
          <button class="btn btn-xs" data-act="escalate" data-id="${i.id}" ${closed ? "disabled" : ""}>Eskalieren</button>
          <button class="btn btn-xs btn-danger" data-act="close" data-id="${i.id}" ${closed ? "disabled" : ""}>Schließen</button>
        </div></td>`;
      tr.querySelector('[data-act="edit"]').onclick = () => openIncidentModal(i);
      tr.querySelector('[data-act="escalate"]').onclick = () => openEscalateModal(i);
      tr.querySelector('[data-act="close"]').onclick = () => closeIncident(i.id);
      tbody.appendChild(tr);
    }
  } catch (e) {
    toast(e.message, "err");
  }
}

function openIncidentModal(incident) {
  const editing = Boolean(incident);
  const sev = editing ? sevName(incident.severity) : "Medium";
  const st = editing ? stName(incident.status) : "Open";

  const body = `
    <div class="form-grid">
      <label class="full">Titel
        <input id="f-title" maxlength="150" value="${editing ? esc(incident.title) : ""}" required />
      </label>
      <label class="full">Beschreibung
        <textarea id="f-desc" required>${editing ? esc(incident.description) : ""}</textarea>
      </label>
      <label>Betroffenes System
        <input id="f-system" value="${editing ? esc(incident.affectedSystem) : ""}" required />
      </label>
      <label>CVE (optional)
        <input id="f-cve" placeholder="CVE-2024-12345" value="${editing && incident.cveNumber ? esc(incident.cveNumber) : ""}" />
      </label>
      <label>Schweregrad
        ${selectHtml("f-sev", SEVERITIES, sev)}
      </label>
      ${editing ? `<label>Status ${selectHtml("f-status", STATUSES, st)}</label>` : `
      <label>Gemeldet von
        <input id="f-reporter" value="" required />
      </label>`}
      ${editing ? `
      <label class="full">Zugewiesen an (optional)
        <input id="f-assigned" value="${incident.assignedTo ? esc(incident.assignedTo) : ""}" />
      </label>` : ""}
    </div>
    <div class="modal-actions">
      <button class="btn btn-ghost" data-close>Abbrechen</button>
      <button class="btn btn-primary" id="save-incident">${editing ? "Speichern" : "Anlegen"}</button>
    </div>`;

  openModal(editing ? `Vorfall #${incident.id} bearbeiten` : "Neuer Vorfall", body);

  $("#save-incident").onclick = async () => {
    try {
      if (editing) {
        await api(`/incidents/${incident.id}`, {
          method: "PUT",
          body: {
            title: $("#f-title").value.trim(),
            description: $("#f-desc").value.trim(),
            affectedSystem: $("#f-system").value.trim(),
            cveNumber: $("#f-cve").value.trim() || null,
            severity: $("#f-sev").value,
            status: $("#f-status").value,
            assignedTo: $("#f-assigned").value.trim() || null,
          },
        });
        toast(`Vorfall #${incident.id} aktualisiert.`);
      } else {
        const created = await api("/incidents", {
          method: "POST",
          body: {
            title: $("#f-title").value.trim(),
            description: $("#f-desc").value.trim(),
            affectedSystem: $("#f-system").value.trim(),
            cveNumber: $("#f-cve").value.trim() || null,
            severity: $("#f-sev").value,
            reportedBy: $("#f-reporter").value.trim(),
          },
        });
        toast(`Vorfall #${created.id} angelegt.`);
      }
      closeModal();
      loadIncidents();
    } catch (e) {
      toast(e.message, "err");
    }
  };
}

function openEscalateModal(incident) {
  const body = `
    <p class="muted">Vorfall <b>#${incident.id}</b> — ${esc(incident.title)}</p>
    <label>Neuer Bearbeiter / Level
      <input id="f-target" placeholder="z.B. tier2@example.com" required />
    </label>
    <label>Begründung
      <textarea id="f-reason" placeholder="Warum wird eskaliert?" required></textarea>
    </label>
    <div class="modal-actions">
      <button class="btn btn-ghost" data-close>Abbrechen</button>
      <button class="btn btn-primary" id="do-escalate">Eskalieren</button>
    </div>`;
  openModal("Vorfall eskalieren", body);

  $("#do-escalate").onclick = async () => {
    try {
      await api(`/incidents/${incident.id}/escalate`, {
        method: "POST",
        body: { targetAssignee: $("#f-target").value.trim(), reason: $("#f-reason").value.trim() },
      });
      toast(`Vorfall #${incident.id} eskaliert.`);
      closeModal();
      loadIncidents();
    } catch (e) {
      toast(e.message, "err");
    }
  };
}

async function closeIncident(id) {
  if (!confirm(`Vorfall #${id} wirklich schließen?`)) return;
  try {
    await api(`/incidents/${id}/close`, { method: "POST" });
    toast(`Vorfall #${id} geschlossen.`);
    loadIncidents();
  } catch (e) {
    toast(e.message, "err");
  }
}

// ───────────────────────────── logs ─────────────────────────────
async function loadLogs() {
  const count = $("#log-count").value;
  try {
    const logs = await api(`/logs?count=${count}`);
    const tbody = $("#log-rows");
    tbody.innerHTML = "";
    $("#log-empty").hidden = logs.length > 0;
    for (const l of logs) {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${fmtDate(l.timestamp)}</td>
        <td class="lvl-${esc(l.logLevel)}">${esc(l.logLevel)}</td>
        <td>${esc(l.message)}</td>
        <td class="muted">${l.details ? esc(l.details) : "—"}</td>`;
      tbody.appendChild(tr);
    }
  } catch (e) {
    toast(e.message, "err");
  }
}

// ───────────────────────────── users ─────────────────────────────
async function loadUsers() {
  try {
    const users = await api("/auth/users");
    const tbody = $("#user-rows");
    tbody.innerHTML = "";
    for (const u of users) {
      const active = u.isActive;
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>${u.id}</td>
        <td>${esc(u.username)}</td>
        <td>${esc(u.email)}</td>
        <td>${esc(roleName(u.role))}</td>
        <td class="${active ? "pill-ok" : "pill-off"}">${active ? "● aktiv" : "○ inaktiv"}</td>
        <td>${fmtDate(u.createdAt)}</td>
        <td><div class="row-actions">
          <button class="btn btn-xs ${active ? "btn-danger" : ""}" data-id="${u.id}">
            ${active ? "Deaktivieren" : "Aktivieren"}
          </button>
        </div></td>`;
      tr.querySelector("button").onclick = () => toggleUser(u.id, !active);
      tbody.appendChild(tr);
    }
  } catch (e) {
    toast(e.message, "err");
  }
}

async function toggleUser(id, isActive) {
  try {
    await api(`/auth/users/${id}/status?isActive=${isActive}`, { method: "PUT" });
    toast(`Benutzer #${id} ${isActive ? "aktiviert" : "deaktiviert"}.`);
    loadUsers();
  } catch (e) {
    toast(e.message, "err");
  }
}

function openUserModal() {
  const body = `
    <div class="form-grid">
      <label>Benutzername
        <input id="u-name" minlength="3" maxlength="100" required />
      </label>
      <label>Rolle ${selectHtml("u-role", ROLES, "User")}</label>
      <label class="full">E-Mail
        <input id="u-email" type="email" required />
      </label>
      <label class="full">Passwort (min. 8 Zeichen)
        <input id="u-pass" type="password" minlength="8" required />
      </label>
    </div>
    <div class="modal-actions">
      <button class="btn btn-ghost" data-close>Abbrechen</button>
      <button class="btn btn-primary" id="save-user">Anlegen</button>
    </div>`;
  openModal("Neuer Benutzer", body);

  $("#save-user").onclick = async () => {
    try {
      await api("/auth/register", {
        method: "POST",
        body: {
          username: $("#u-name").value.trim(),
          email: $("#u-email").value.trim(),
          password: $("#u-pass").value,
          role: $("#u-role").value,
        },
      });
      toast("Benutzer angelegt.");
      closeModal();
      loadUsers();
    } catch (e) {
      toast(e.message, "err");
    }
  };
}

// ───────────────────────────── modal plumbing ─────────────────────────────
function selectHtml(id, options, selected) {
  return `<select id="${id}">${options
    .map((o) => `<option value="${o}" ${o === selected ? "selected" : ""}>${o}</option>`)
    .join("")}</select>`;
}

function openModal(title, bodyHtml) {
  $("#modal-title").textContent = title;
  $("#modal-body").innerHTML = bodyHtml;
  $("#modal-backdrop").hidden = false;
  $$("[data-close]", $("#modal-body")).forEach((b) => (b.onclick = closeModal));
}
function closeModal() { $("#modal-backdrop").hidden = true; $("#modal-body").innerHTML = ""; }

// ───────────────────────────── wiring ─────────────────────────────
function init() {
  $("#login-form").addEventListener("submit", doLogin);
  $("#logout-btn").addEventListener("click", logout);
  $$(".tab").forEach((t) => t.addEventListener("click", () => switchTab(t.dataset.tab)));
  $("#new-incident-btn").addEventListener("click", () => openIncidentModal(null));
  $("#filter-status").addEventListener("change", loadIncidents);
  $("#filter-severity").addEventListener("change", loadIncidents);
  $("#refresh-logs-btn").addEventListener("click", loadLogs);
  $("#log-count").addEventListener("change", loadLogs);
  $("#new-user-btn").addEventListener("click", openUserModal);
  $("#modal-close").addEventListener("click", closeModal);
  $("#modal-backdrop").addEventListener("click", (e) => { if (e.target.id === "modal-backdrop") closeModal(); });

  // restore a previous session if present
  try {
    const saved = sessionStorage.getItem("sims_user");
    if (saved) { state.user = JSON.parse(saved); enterApp(); }
  } catch { /* ignore */ }
}

document.addEventListener("DOMContentLoaded", init);
