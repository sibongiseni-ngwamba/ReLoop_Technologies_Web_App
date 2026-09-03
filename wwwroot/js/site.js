const toneClass = (tone) => `reloop-badge reloop-badge--${tone || "neutral"}`;

async function fetchJson(url, options) {
  const response = await fetch(url, options);
  const payload = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw Object.assign(new Error(payload.title || "Please check the highlighted fields and try again."), { payload });
  }
  return payload;
}

function readForm(form) {
  const data = new FormData(form);
  const body = {};
  for (const [key, value] of data.entries()) body[key] = value;
  form.querySelectorAll('input[type="checkbox"]').forEach((input) => body[input.name] = input.checked);
  return body;
}

function showFormErrors(form, errors = {}) {
  form.querySelectorAll("[data-error-for]").forEach((item) => {
    item.textContent = errors[item.dataset.errorFor]?.[0] || "";
  });
}

document.querySelectorAll(".js-api-form").forEach((form) => {
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const status = form.querySelector(".form-status");
    showFormErrors(form);

    if (!form.checkValidity()) {
      status.textContent = "Please complete the required fields.";
      form.reportValidity();
      return;
    }

    try {
      await fetchJson(form.dataset.endpoint, {
        method: form.dataset.method || "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(readForm(form))
      });
      status.textContent = form.dataset.success || "Saved successfully.";
      status.classList.add("is-success");
      if (form.dataset.redirect) window.setTimeout(() => window.location.assign(form.dataset.redirect), 600);
    } catch (error) {
      status.textContent = error.message;
      showFormErrors(form, error.payload?.errors);
    }
  });
});

async function loadProfile() {
  const root = document.querySelector(".js-profile");
  if (!root) return;

  const profile = await fetchJson("/api/profile");
  root.querySelectorAll("[data-profile-field]").forEach((field) => {
    field.value = profile[field.dataset.profileField] || "";
  });
  root.querySelector("[data-profile-summary]").innerHTML = `
    <div class="profile-stat"><span>Role</span><strong>${profile.role}</strong></div>
    <div class="profile-stat"><span>Reward Points</span><strong>${profile.rewardPoints}</strong></div>
    <div class="profile-stat"><span>Scans</span><strong>${profile.scanCount}</strong></div>
    <div class="profile-stat"><span>Pickups</span><strong>${profile.pickupCount}</strong></div>`;
}

document.querySelectorAll(".js-logout").forEach((button) => {
  button.addEventListener("click", async () => {
    const status = button.parentElement.querySelector(".form-status");
    await fetchJson("/api/auth/logout", { method: "POST" });
    status.textContent = "You are logged out. Redirecting to the home page...";
    status.classList.add("is-success");
    window.setTimeout(() => window.location.assign("/"), 700);
  });
});

function metricCard(metric) {
  return `<article class="metric-card metric-card--${metric.tone}"><span>${metric.label}</span><strong>${metric.value}</strong><small>${metric.detail}</small></article>`;
}

function statusTone(status) {
  return status === "Completed" ? "success" : status === "Cancelled" ? "danger" : status === "Scheduled" ? "info" : "warning";
}

function pickupMini(pickup) {
  return `<article class="pickup-row"><strong>${pickup.id}</strong><span>${pickup.wasteType}</span><span>${pickup.date}</span><span class="${toneClass(statusTone(pickup.status))}">${pickup.status}</span></article>`;
}

async function loadDashboard() {
  const root = document.querySelector(".js-dashboard");
  if (!root) return;
  const data = await fetchJson("/api/dashboard");
  root.querySelector("[data-dashboard-metrics]").innerHTML = data.metrics.map(metricCard).join("");
  root.querySelector("[data-dashboard-activity]").innerHTML = data.activity.map(item => `<article class="activity-item"><span class="${toneClass(item.tone)}">${item.date}</span><strong>${item.title}</strong><p>${item.detail}</p></article>`).join("");
  root.querySelector("[data-dashboard-pickups]").innerHTML = data.pickups.map(pickupMini).join("");
}

document.querySelectorAll(".js-scan-form").forEach((form) => {
  const input = form.querySelector('input[type="file"]');
  const label = form.querySelector(".upload-zone__drop strong");
  const status = form.querySelector(".form-status");
  const result = document.querySelector("[data-scan-result]");

  input.addEventListener("change", () => label.textContent = input.files[0]?.name || "Drag & drop an image or click to upload");
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!input.files.length) {
      status.textContent = "Choose an image before classifying.";
      return;
    }

    const payload = await fetchJson("/api/scan/classify", { method: "POST", body: new FormData(form) });
    status.textContent = "Scanning complete. Highly confident classification matches.";
    status.classList.add("is-success");
    result.hidden = false;
    result.innerHTML = `<div class="scan-result__media">${input.files[0].name}</div><div class="scan-result__details"><h2>${payload.item}</h2><div class="badge-row"><span class="reloop-badge reloop-badge--success">${payload.disposition}</span><span class="reloop-badge reloop-badge--warning">+${payload.points} pts</span><span class="reloop-badge reloop-badge--info">${payload.confidence}% confidence</span></div><dl><dt>Category</dt><dd>${payload.category}</dd><dt>Estimated Weight</dt><dd>${payload.estimatedWeight}</dd><dt>Date Scanned</dt><dd>${new Date(payload.dateScanned).toLocaleString()}</dd></dl><div class="button-pair"><button class="reloop-btn reloop-btn--primary" type="button">Save Result</button><button class="reloop-btn reloop-btn--secondary" type="button" onclick="location.reload()">Scan Another Item</button></div></div>`;
  });
});

async function loadPickups(status = "All") {
  const root = document.querySelector(".js-pickups");
  if (!root) return;
  const rows = await fetchJson(`/api/pickups?status=${encodeURIComponent(status)}`);
  root.querySelector("[data-pickups-table]").innerHTML = rows.map(row => `<tr><td><strong>${row.id}</strong></td><td>${row.date}</td><td>${row.address}</td><td><strong>${row.wasteType}</strong></td><td>${row.weight}</td><td><span class="${toneClass(statusTone(row.status))}">${row.status}</span></td></tr>`).join("");
  root.querySelector("[data-pickups-empty]").hidden = rows.length > 0;
}

document.querySelectorAll(".js-pickups .tab").forEach((button) => {
  button.addEventListener("click", () => {
    document.querySelectorAll(".js-pickups .tab").forEach((tab) => tab.classList.remove("is-active"));
    button.classList.add("is-active");
    loadPickups(button.dataset.status);
  });
});

async function loadAdmin() {
  const root = document.querySelector(".js-admin");
  if (!root) return;
  const data = await fetchJson("/api/admin/stats");
  root.querySelector("[data-admin-metrics]").innerHTML = data.metrics.map(metricCard).join("");
  root.querySelector("[data-admin-legend]").innerHTML = data.wasteDistribution.map(slice => `<span><i class="legend-dot legend-dot--${slice.tone}"></i>${slice.type}<strong>${slice.percent}%</strong></span>`).join("");
  root.querySelector("[data-admin-table]").innerHTML = data.pendingPickups.map(row => `<tr><td><strong>Alex Rivera</strong></td><td>${row.date}</td><td>${row.wasteType}</td><td>${row.weight}</td><td><span class="${toneClass(statusTone(row.status))}">${row.status}</span></td></tr>`).join("");
}

loadDashboard();
loadPickups();
loadAdmin();
loadProfile();
