(() => {
  const config = window.RELOOP_FIREBASE_CONFIG || {};
  const configPresent = window.ReLoopDomain.isValidWebFirebaseConfig(config);
  const sdkPresent = typeof firebase !== "undefined";
  const configured = configPresent && sdkPresent;
  const app = configured ? (firebase.apps.length ? firebase.app() : firebase.initializeApp(config)) : null;
  const auth = app ? firebase.auth(app) : null;
  const db = app ? firebase.firestore(app) : null;
  let currentUser = null;
  let currentProfile = null;
  let authStateResolved = false;
  let resolveAuthReady;
  const authReady = new Promise((resolve) => { resolveAuthReady = resolve; });
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const esc = (value = "") => String(value).replace(/[&<>"']/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[char]));
  const formValues = (form) => Object.fromEntries(new FormData(form).entries());
  const msg = (node, value, ok = false) => { if (node) { node.textContent = value; node.classList.toggle("is-success", ok); } };
  const unavailable = () => { throw new Error(configPresent ? "The Firebase browser SDK did not load. Check your internet connection and allow https://www.gstatic.com." : "Firebase web access is not configured. Register a Web app in Firebase Console and set Firebase:ApiKey, Firebase:AuthDomain, Firebase:ProjectId, and Firebase:AppId in appsettings.Development.json."); };
  const needsDb = () => { if (!auth || !db) unavailable(); };
  const profileRef = (user) => db.collection("users").doc(user.uid);
  const profileData = (user) => profileRef(user).get().then(async (doc) => {
    if (!doc.exists) {
      const data = { firstName: user.displayName?.split(" ")[0] || "", lastName: user.displayName?.split(" ").slice(1).join(" ") || "", email: user.email || "", phoneNumber: "", address: "", role: "resident", isActive: true, createdAt: Date.now(), updatedAt: Date.now() };
      await profileRef(user).set(data); return data;
    }
    return doc.data();
const toneClass = (tone) => `reloop-badge reloop-badge--${tone || "neutral"}`;

async function fetchJson(url, options) {
  let response;
  try { response = await fetch(url, options); } catch { throw new Error("Network unavailable. Check your connection and try again."); }
  if (response.status === 401) {
    window.location.assign(`/Login?returnUrl=${encodeURIComponent(window.location.pathname)}`);
    throw new Error("Your session has expired. Please sign in again.");
  }
  if (response.status === 403) throw new Error("You do not have permission to perform this action.");
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
  const requireSignedIn = async () => {
    needsDb();
    if (!authStateResolved) await authReady;
    const user = currentUser || auth.currentUser;
    if (!user) { location.assign("/Login"); throw new Error("Sign in to continue."); }
    const profile = currentProfile || await profileData(user);
    currentProfile = profile;
    if (profile.isActive === false) { await auth.signOut(); location.assign("/Login"); throw new Error("This account is inactive. Contact ReLoop support."); }
    return { user, profile };
  };
  const applyShellAccess = (user, profile) => {
    const signedIn = Boolean(user);
    const role = String(profile?.role || "resident").toLowerCase();
    $$('[data-auth-nav]').forEach((item) => { const roles = (item.dataset.roles || "").split(","); item.hidden = !signedIn || !roles.includes(role); });
    $$('[data-admin-nav]').forEach((item) => { item.hidden = !signedIn || !window.ReLoopDomain.isAdminRole(role); });
    $$('[data-guest-nav]').forEach((item) => { item.hidden = signedIn; });
    const avatar = $(".reloop-avatar");
    if (avatar) avatar.textContent = (profile?.firstName || user?.email || "R").slice(0, 1).toUpperCase();
  };
  const authorizeCurrentPage = async (user) => {
    const path = location.pathname;
    let profile = null;
    if (user) {
      const isAuthPage = path.toLowerCase() === "/login" || path.toLowerCase() === "/signup";
      if (isAuthPage && !currentProfile) {
        applyShellAccess(user, null);
        document.body.dataset.accessReady = "true";
        return;
      }
      try { profile = currentProfile || await profileData(user); currentProfile = profile; }
      catch (error) { profile = null; }
    }
    applyShellAccess(user, profile);
    const access = window.ReLoopDomain.accessForPath(path, Boolean(user), profile?.role);
    document.body.dataset.accessReady = "true";
    if (access === "sign-in") { location.replace(`/Login?returnUrl=${encodeURIComponent(path)}`); return; }
    if (access === "forbidden") { location.replace("/Dashboard"); return; }
    if (access === "signed-in-only") { location.replace(window.ReLoopDomain.isAdminRole(profile?.role) ? "/Admin/Index" : "/Dashboard"); }
  };
  const friendlyError = (error) => {
    const messages = {
      "auth/invalid-api-key": "Firebase rejected the Web app API key. Check the Web app values in appsettings.Development.json.",
      "auth/unauthorized-domain": "This site host is not authorized. Add localhost under Firebase Authentication → Settings → Authorized domains.",
      "auth/operation-not-allowed": "Google sign-in is not enabled for this Firebase project.",
      "auth/invalid-credential": "The email or password is incorrect.",
      "auth/email-already-in-use": "That email already has an account. Try logging in instead.",
      "auth/weak-password": "Choose a stronger password for your account.",
      "auth/popup-closed-by-user": "Google sign-in was cancelled.",
      "auth/popup-blocked": "Your browser blocked the Google sign-in window. Allow popups and try again.",
      "auth/network-request-failed": "Could not reach Firebase. Check your internet connection and try again.",
      "permission-denied": "Firebase Authentication succeeded, but Firestore denied access. Check the users collection rules and this account's profile."
    };
    return messages[error?.code] || error?.message || "Firebase could not complete that request.";
  };
  const rows = async (name, query = null) => { const snap = await (query || db.collection(name)).get(); return snap.docs.map((doc) => ({ id: doc.id, ...doc.data() })); };
  const postContact = async (form) => { needsDb(); const values = formValues(form); const ref = db.collection("contactMessages").doc(); await ref.set({ contactMessageId: ref.id, userId: auth.currentUser?.uid || null, fullName: values.fullName.trim(), email: values.email.trim(), subject: values.subject.trim(), message: values.message.trim(), status: "New", createdAt: Date.now() }); };
  const getPickups = (userId, status = "All") => {
    let query = db.collection("pickups").where("userId", "==", userId);
    if (status !== "All") query = query.where("status", "==", status);
    return rows("pickups", query);
  };
  const dateText = (value) => { const date = value?.toDate ? value.toDate() : new Date(Number(value) || value || Date.now()); return Number.isNaN(date.valueOf()) ? "—" : date.toLocaleString(); };
  const pickupStatusTone = (status) => status === "Completed" ? "success" : status === "Cancelled" ? "danger" : status === "Scheduled" ? "info" : "warning";

  const finishAuth = async (user) => {
    const profile = await profileData(user);
    if (profile.isActive === false) { await auth.signOut(); throw new Error("This account is inactive. Contact ReLoop support."); }
    currentUser = user; currentProfile = profile;
    const requested = new URLSearchParams(location.search).get("returnUrl");
    const safeReturn = requested?.startsWith("/") && !requested.startsWith("//") ? requested : null;
    location.assign(safeReturn || (window.ReLoopDomain.isAdminRole(profile.role) ? "/Admin/Index" : "/Dashboard"));
  };

  $$(".js-api-form").filter((form) => ["/api/auth/login", "/api/auth/signup"].includes(form.dataset.endpoint)).forEach((form) => form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const status = $(".form-status", form); msg(status, "");
    if (!configured) { msg(status, "Firebase is not configured. See README.md for the Web app settings."); return; }
    if (!form.reportValidity()) return;
    try {
      const values = formValues(form);
      if (form.dataset.endpoint === "/api/auth/signup") {
        if (values.password !== values.confirmPassword) throw new Error("Passwords do not match.");
        const credential = await auth.createUserWithEmailAndPassword(values.email.trim(), values.password);
        const { firstName, lastName } = window.ReLoopDomain.splitName(values.fullName);
        await profileRef(credential.user).set({ firstName, lastName, email: credential.user.email, phoneNumber: "", address: "", role: "resident", isActive: true, createdAt: Date.now(), updatedAt: Date.now() });
        await finishAuth(credential.user);
      } else {
        const credential = await auth.signInWithEmailAndPassword(values.email.trim(), values.password);
        await finishAuth(credential.user);
      }
    } catch (error) { msg(status, friendlyError(error)); }
  }));

  $$(".js-google-signin").forEach((button) => button.addEventListener("click", async () => {
    const status = $("[data-google-status]", button.closest(".auth-card"));
    if (!configured) { msg(status, "Firebase is not configured. See README.md for the Web app settings."); return; }
    button.disabled = true;
    msg(status, "Opening Google sign-in…");
    try {
      const provider = new firebase.auth.GoogleAuthProvider();
      provider.setCustomParameters({ prompt: "select_account" });
      const result = await auth.signInWithPopup(provider);
      const user = result.user;
      if (!user) throw new Error("Google sign-in completed without a Firebase user.");
      await finishAuth(user);
    } catch (error) {
      msg(status, friendlyError(error));
      button.disabled = false;
    }
  }));

  $$(".js-api-form").forEach((form) => form.addEventListener("submit", async (event) => {
    event.preventDefault(); const status = $(".form-status", form); msg(status, "");
    if (!configured) { msg(status, "Firebase is not configured. See the setup note in README.md."); return; }
    if (!form.reportValidity()) return;
    try {
      if (form.dataset.endpoint === "/api/profile") {
        const { user, profile } = await requireSignedIn(); const values = formValues(form);
        await profileRef(user).set({ ...profile, ...window.ReLoopDomain.splitName(values.fullName), email: user.email, address: values.address, preferredCategory: values.preferredCategory, updatedAt: Date.now() }, { merge: true });
      } else if (form.dataset.endpoint === "/api/pickups") {
        const { user } = await requireSignedIn(); const values = formValues(form);
        const scheduledDate = new Date(`${values.preferredDate}T${values.preferredTimeWindow.slice(0, 5)}:00`);
        const ref = db.collection("pickups").doc();
        await ref.set({ pickupId: ref.id, userId: user.uid, agentId: null, scheduledDate: scheduledDate.getTime(), wasteType: values.wasteCategory, wasteTypes: [values.wasteCategory], address: values.homeAddress, notes: values.notes || "", status: "Scheduled", createdAt: Date.now(), updatedAt: Date.now() });
      } else if (form.dataset.endpoint === "/api/contact") await postContact(form);
      else { unavailable(); }
      msg(status, form.dataset.success || "Saved successfully.", true);
      if (form.dataset.endpoint === "/api/profile") loadProfile();
    } catch (error) { msg(status, friendlyError(error)); }
  }));

  $$(".js-logout").forEach((button) => button.addEventListener("click", async () => { try { needsDb(); await auth.signOut(); location.assign("/"); } catch (e) { msg($(".form-status", button.parentElement), e.message); } }));

  const metricCard = (metric) => `<article class="metric-card metric-card--${esc(metric.tone || "info")}"><span>${esc(metric.label)}</span><strong>${esc(metric.value)}</strong><small>${esc(metric.detail || "")}</small></article>`;
  const loadProfile = async () => {
    const root = $(".js-profile"); if (!root) return;
    try { const { user, profile } = await requireSignedIn(); const [scans, pickups, rewards] = await Promise.all([rows("wasteLogs", db.collection("wasteLogs").where("userId", "==", user.uid)), getPickups(user.uid), db.collection("rewards").doc(user.uid).get()]);
      const fullName = `${profile.firstName || ""} ${profile.lastName || ""}`.trim();
      $$("[data-profile-field]", root).forEach((field) => { field.value = field.dataset.profileField === "fullName" ? fullName : field.dataset.profileField === "email" ? (user.email || "") : profile[field.dataset.profileField] || ""; });
      $("[data-profile-summary]", root).innerHTML = `<div class="profile-stat"><span>Role</span><strong>${esc(profile.role || "resident")}</strong></div><div class="profile-stat"><span>Reward Points</span><strong>${esc(rewards.data()?.pointsBalance ?? 0)}</strong></div><div class="profile-stat"><span>Scans</span><strong>${scans.length}</strong></div><div class="profile-stat"><span>Pickups</span><strong>${pickups.length}</strong></div>`;
    } catch (e) { const target = $("[data-profile-summary]", root); if (target) target.textContent = e.message; }
  };
  const loadDashboard = async () => {
    const root = $(".js-dashboard"); if (!root) return;
    try { const { user, profile } = await requireSignedIn(); const [pickups, scans, rewards, txs] = await Promise.all([getPickups(user.uid), rows("wasteLogs", db.collection("wasteLogs").where("userId", "==", user.uid)), db.collection("rewards").doc(user.uid).get(), rows("pointsTransactions", db.collection("pointsTransactions").where("userId", "==", user.uid))]);
      const upcoming = pickups.filter((p) => p.status === "Scheduled").slice(0, 3);
      const name = `${profile.firstName || ""} ${profile.lastName || ""}`.trim() || "there";
      const heading = $(".page-heading h1", root); if (heading) heading.textContent = `Hello, ${name}. Your recycling loop is moving.`;
      $("[data-dashboard-metrics]", root).innerHTML = [metricCard({ label: "Reward Points", value: `${rewards.data()?.pointsBalance || 0} pts`, tone: "success" }), metricCard({ label: "Items Scanned", value: scans.length, tone: "info" }), metricCard({ label: "Weight Diverted", value: `${scans.reduce((sum, s) => sum + Number(s.weightKg || 0), 0).toFixed(1)} kg`, tone: "warning" }), metricCard({ label: "Scheduled Pickups", value: upcoming.length, tone: "neutral" })].join("");
      $("[data-dashboard-activity]", root).innerHTML = txs.slice(0, 6).map((tx) => `<article class="activity-item"><span class="reloop-badge reloop-badge--success">${dateText(tx.createdAt)}</span><strong>${esc(tx.description || tx.transactionType || "Points updated")}</strong><p>${esc(tx.points || 0)} points</p></article>`).join("") || "<p>No recent activity yet.</p>";
      $("[data-dashboard-pickups]", root).innerHTML = upcoming.map((p) => `<article class="pickup-row"><strong>${esc(p.pickupId || p.id)}</strong><span>${esc(p.wasteType || p.wasteTypes?.join(", "))}</span><span>${dateText(p.scheduledDate)}</span><span class="reloop-badge reloop-badge--info">${esc(p.status)}</span></article>`).join("") || "<p>No upcoming pickups.</p>";
    } catch (e) { const target = $("[data-dashboard-metrics]", root); if (target) target.innerHTML = `<p class="empty-state">${esc(e.message)}</p>`; }
  };
  const renderPickups = async (status = "All") => { const root = $(".js-pickups"); if (!root) return; try { const { user, profile } = await requireSignedIn(); const isAgent = String(profile.role).toLowerCase() === "agent"; const heading = $(".page-heading h1", root); if (isAgent && heading) heading.textContent = "Pickup Operations"; let query = isAgent ? db.collection("pickups") : db.collection("pickups").where("userId", "==", user.uid); if (status !== "All") query = query.where("status", "==", status); const list = await rows("pickups", query); $("[data-pickups-table]", root).innerHTML = list.map((p) => { const statusCell = isAgent ? `<select data-agent-status="${esc(p.id)}" data-current-status="${esc(p.status)}">${["Scheduled", "In Progress", "Completed", "Cancelled"].map((s) => `<option ${p.status === s ? "selected" : ""}>${s}</option>`).join("")}</select><button type="button" class="reloop-btn reloop-btn--secondary" data-agent-save-pickup="${esc(p.id)}">Update</button>` : `<span class="reloop-badge reloop-badge--${pickupStatusTone(p.status)}">${esc(p.status)}</span>`; return `<tr><td><strong>${esc(p.pickupId || p.id)}</strong></td><td>${dateText(p.scheduledDate)}</td><td>${esc(p.address)}</td><td><strong>${esc(p.wasteType || p.wasteTypes?.join(", "))}</strong></td><td>—</td><td>${statusCell}</td></tr>`; }).join(""); $("[data-pickups-empty]", root).hidden = list.length > 0; } catch (e) { $("[data-pickups-empty]", root).hidden = false; $("[data-pickups-empty]", root).textContent = friendlyError(e); } };
  $$(".js-pickups .tab").forEach((button) => button.addEventListener("click", () => { $$(".js-pickups .tab").forEach((tab) => tab.classList.remove("is-active")); button.classList.add("is-active"); renderPickups(button.dataset.status); }));
  document.addEventListener("click", async (event) => { const button = event.target.closest("[data-agent-save-pickup]"); if (!button) return; try { const { profile } = await requireSignedIn(); if (String(profile.role).toLowerCase() !== "agent") throw new Error("Only collection agents can update pickup operations."); const pickupId = button.dataset.agentSavePickup; const picker = $(`[data-agent-status="${CSS.escape(pickupId)}"]`); const previous = picker.dataset.currentStatus; const next = picker.value; if (!window.ReLoopDomain.canTransitionPickup(previous, next)) throw new Error(`A pickup cannot move from ${previous} to ${next}.`); const updates = { status: next, updatedAt: Date.now() }; if (next === "Completed") updates.completedAt = Date.now(); await db.collection("pickups").doc(pickupId).update(updates); await renderPickups(); } catch (error) { const empty = $("[data-pickups-empty]"); empty.hidden = false; empty.textContent = friendlyError(error); } });

  $$(".js-scan-form").forEach((form) => { const input = $("input[type=file]", form); const label = $(".upload-zone__drop strong", form); const status = $(".form-status", form); const result = $("[data-scan-result]"); input?.addEventListener("change", () => { label.textContent = input.files[0]?.name || "Drag & drop an image or click to upload"; }); form.addEventListener("submit", async (event) => { event.preventDefault(); if (!input.files.length) { msg(status, "Choose an image before classifying."); return; } try { const { user } = await requireSignedIn(); const file = input.files[0]; const name = file.name.toLowerCase(); const category = name.includes("glass") ? "Glass Bottles" : name.includes("paper") ? "Paper" : "Plastics (PET 1)"; const itemName = category === "Paper" ? "Cardboard Sheet" : category === "Glass Bottles" ? "Glass Jar" : "Plastic Bottle"; const ref = db.collection("wasteLogs").doc(); await ref.set({ wasteLogId: ref.id, userId: user.uid, wasteCategoryId: category, modelLabel: itemName, confidenceScore: 94, weightKg: 0.2, createdAt: Date.now(), scannedAt: Date.now(), fileName: file.name }); msg(status, "Scan recorded. Classification is a filename based demonstration.", true); result.hidden = false; result.innerHTML = `<div class="scan-result__media">${esc(file.name)}</div><div class="scan-result__details"><h2>${esc(itemName)}</h2><div class="badge-row"><span class="reloop-badge reloop-badge--success">Recyclable</span><span class="reloop-badge reloop-badge--info">94% confidence</span></div><dl><dt>Category</dt><dd>${esc(category)}</dd><dt>Estimated Weight</dt><dd>0.2 kg</dd><dt>Date Scanned</dt><dd>${dateText(Date.now())}</dd></dl></div>`; } catch (e) { msg(status, e.message); } }); });

  const adminStatus = $("[data-admin-status]");
  const renderAdmin = async () => {
    const root = $(".js-admin"); if (!root) return;
    try { const { profile } = await requireSignedIn(); if (!window.ReLoopDomain.isAdminRole(profile.role)) throw new Error("Administrator access is required."); const workspace = $("[data-admin-workspace]", root); workspace.hidden = false; msg(adminStatus, "Connected to the shared ReLoop Firestore database.", true);
      const [users, pickups, rewards, claims, scans] = await Promise.all([rows("users"), rows("pickups"), rows("rewardItems"), rows("redemptionLogs"), rows("wasteLogs")]);
      const totalKg = pickups.reduce((sum, p) => sum + Number(p.weightKg || 0), 0);
      $("[data-admin-metrics]", root).innerHTML = [metricCard({label:"Users", value:users.length, tone:"success"}),metricCard({label:"Pickup Requests", value:pickups.length, tone:"info"}),metricCard({label:"Waste Scans", value:scans.length, tone:"warning"}),metricCard({label:"Diverted Weight", value:`${totalKg.toFixed(1)} kg`, tone:"neutral"})].join("");
      const dist = Object.entries(scans.reduce((map, scan) => { const key = scan.wasteCategoryId || scan.modelLabel || "Other"; map[key] = (map[key] || 0) + 1; return map; }, {})); const scanTotal = Math.max(scans.length, 1);
      $("[data-admin-legend]", root).innerHTML = dist.map(([name, count]) => `<span><i class="legend-dot legend-dot--info"></i>${esc(name)}<strong>${Math.round(count * 100 / scanTotal)}%</strong></span>`).join("") || "<span>No scan data yet.</span>";
      $("[data-admin-table]", root).innerHTML = pickups.slice(0, 8).map((p) => `<tr><td><strong>${esc(p.userId)}</strong></td><td>${dateText(p.createdAt)}</td><td>${esc(p.wasteType || p.wasteTypes?.join(", "))}</td><td>${esc(p.weightKg ?? "—")}</td><td><span class="reloop-badge reloop-badge--${pickupStatusTone(p.status)}">${esc(p.status)}</span></td></tr>`).join("");
      $("[data-admin-users]", root).innerHTML = users.map((u) => `<tr><td>${esc(`${u.firstName || ""} ${u.lastName || ""}`.trim() || u.userId || u.id)}</td><td>${esc(u.email)}</td><td><select data-user-role="${esc(u.id)}"><option value="resident" ${u.role === "resident" ? "selected" : ""}>Resident</option><option value="agent" ${u.role === "agent" ? "selected" : ""}>Agent</option><option value="admin" ${u.role === "admin" ? "selected" : ""}>Admin</option></select></td><td><input type="checkbox" data-user-active="${esc(u.id)}" ${u.isActive !== false ? "checked" : ""}></td><td><button type="button" class="reloop-btn reloop-btn--secondary" data-save-user="${esc(u.id)}">Save</button></td></tr>`).join("");
      $("[data-admin-pickups]", root).innerHTML = pickups.map((p) => `<tr><td>${esc(p.pickupId || p.id)}</td><td>${esc(p.userId)}</td><td>${dateText(p.scheduledDate)}</td><td>${esc(p.wasteType || p.wasteTypes?.join(", "))}</td><td><select data-pickup-status="${esc(p.id)}">${["Scheduled","In Progress","Completed","Cancelled"].map((s) => `<option ${p.status === s ? "selected" : ""}>${s}</option>`).join("")}</select></td><td><button type="button" class="reloop-btn reloop-btn--secondary" data-save-pickup="${esc(p.id)}">Save</button></td></tr>`).join("");
      $("[data-admin-rewards]", root).innerHTML = rewards.map((r) => `<tr><td>${esc(r.itemName)}</td><td>${esc(r.pointsCost)}</td><td>${esc(r.stockQuantity ?? "—")}</td><td>${r.isActive === false ? "Inactive" : "Active"}</td><td><button type="button" class="reloop-btn reloop-btn--secondary" data-delete-reward="${esc(r.id)}">Delete</button></td></tr>`).join("");
      $("[data-admin-claims]", root).innerHTML = claims.map((c) => `<tr><td>${esc(c.redemptionId || c.id)}</td><td>${esc(c.userId)}</td><td>${esc(c.rewardItemId)}</td><td>${esc(c.pointsUsed)}</td><td><select data-claim-status="${esc(c.id)}">${["Redeemed","Approved","Rejected","Fulfilled"].map((s) => `<option ${c.status === s ? "selected" : ""}>${s}</option>`).join("")}</select></td><td><button type="button" class="reloop-btn reloop-btn--secondary" data-save-claim="${esc(c.id)}">Save</button></td></tr>`).join("");
      $("[data-admin-scans]", root).innerHTML = scans.map((s) => `<tr><td>${esc(s.id)}</td><td>${esc(s.userId)}</td><td>${esc(s.wasteCategoryId || s.modelLabel)}</td><td>${esc(s.weightKg ?? "—")}</td><td>${dateText(s.scannedAt || s.createdAt)}</td></tr>`).join("");
    } catch (error) { msg(adminStatus, error.message); }
  };
  $("[data-admin-workspace]")?.addEventListener("click", async (event) => { const button = event.target.closest("button"); if (!button) return; try {
    if (button.dataset.saveUser) await db.collection("users").doc(button.dataset.saveUser).update({ role: $(`[data-user-role="${CSS.escape(button.dataset.saveUser)}"]`).value, isActive: $(`[data-user-active="${CSS.escape(button.dataset.saveUser)}"]`).checked, updatedAt: Date.now() });
    if (button.dataset.savePickup) await db.collection("pickups").doc(button.dataset.savePickup).update({ status: $(`[data-pickup-status="${CSS.escape(button.dataset.savePickup)}"]`).value, updatedAt: Date.now() });
    if (button.dataset.saveClaim) await db.collection("redemptionLogs").doc(button.dataset.saveClaim).update({ status: $(`[data-claim-status="${CSS.escape(button.dataset.saveClaim)}"]`).value });
    if (button.dataset.deleteReward) await db.collection("rewardItems").doc(button.dataset.deleteReward).delete();
    if (button.dataset.adminRefresh) { await renderAdmin(); return; }
    await renderAdmin();
  } catch (e) { msg(adminStatus, e.message); } });
  $("[data-admin-create='reward']")?.addEventListener("submit", async (event) => { event.preventDefault(); try { await requireSignedIn(); const values = formValues(event.currentTarget); const ref = db.collection("rewardItems").doc(); await ref.set({ rewardItemId: ref.id, itemName: values.itemName.trim(), description: values.description || "", pointsCost: Number(values.pointsCost), stockQuantity: values.stockQuantity === "" ? null : Number(values.stockQuantity), isActive: true, createdAt: Date.now(), updatedAt: Date.now() }); event.currentTarget.reset(); await renderAdmin(); } catch (e) { msg(adminStatus, e.message); } });
  $("[data-admin-create='claim']")?.addEventListener("submit", async (event) => { event.preventDefault(); try { const { profile } = await requireSignedIn(); if (!window.ReLoopDomain.isAdminRole(profile.role)) throw new Error("Administrator access is required."); const values = formValues(event.currentTarget); const ref = db.collection("redemptionLogs").doc(); const now = Date.now(); await ref.set({ redemptionId: ref.id, userId: values.userId.trim(), rewardItemId: values.rewardItemId.trim(), pointsUsed: Number(values.pointsUsed), voucherCode: null, status: values.status, redeemedAt: now, createdAt: now }); event.currentTarget.reset(); await renderAdmin(); } catch (e) { msg(adminStatus, e.message); } });

  $$("[data-admin-refresh]").forEach((button) => button.addEventListener("click", renderAdmin));
  if (auth) {
    auth.onAuthStateChanged(async (user) => {
      currentUser = user;
      currentProfile = null;
      if (user) {
        try { const snapshot = await profileRef(user).get(); currentProfile = snapshot.exists ? snapshot.data() : null; }
        catch (error) { console.warn("Could not read the Firebase user profile", error.code || error.message); }
      }
      if (!authStateResolved) { authStateResolved = true; resolveAuthReady(user); }
      await authorizeCurrentPage(user);
    });
  } else {
    applyShellAccess(null, null);
    document.body.dataset.accessReady = "true";
    if (document.body.dataset.protectedPage === "true") location.replace("/Login");
    const notice = configPresent ? "Firebase's browser SDK could not load. Check your internet connection and allow https://www.gstatic.com." : "Firebase Web app configuration is missing. Add the web app values to appsettings.Development.json.";
    $$(".js-api-form .form-status").forEach((status) => msg(status, notice));
    if (adminStatus) msg(adminStatus, notice);
  }
  loadDashboard(); renderPickups(); loadProfile(); renderAdmin();
})();

    const submitButton = form.querySelector('button[type="submit"]');
    const initialLabel = submitButton?.innerHTML;
    try {
      if (submitButton) { submitButton.disabled = true; submitButton.classList.add("is-loading"); submitButton.textContent = "Saving…"; }
      status.textContent = "Saving your details…";
      status.classList.remove("is-success");
      status.classList.add("is-loading");
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
    } finally {
      status.classList.remove("is-loading");
      if (submitButton) { submitButton.disabled = false; submitButton.classList.remove("is-loading"); submitButton.innerHTML = initialLabel; }
    }
  });
});

const menuToggle = document.querySelector(".reloop-menu-toggle");
const primaryNavigation = document.querySelector("#primary-navigation");
menuToggle?.addEventListener("click", () => {
  const isOpen = primaryNavigation.classList.toggle("is-open");
  menuToggle.setAttribute("aria-expanded", String(isOpen));
});

document.querySelectorAll("input[type='date'][name='preferredDate']").forEach((input) => {
  input.min = new Date().toISOString().slice(0, 10);
});

async function loadProfile() {
  const root = document.querySelector(".js-profile");
  if (!root) return;

  try {
    const profile = await fetchJson("/api/profile");
    root.querySelectorAll("[data-profile-field]").forEach((field) => {
      field.value = profile[field.dataset.profileField] || "";
    });
    root.querySelector("[data-profile-summary]").innerHTML = `
    <div class="profile-stat"><span>Role</span><strong>${profile.role}</strong></div>
    <div class="profile-stat"><span>Reward Points</span><strong>${profile.rewardPoints}</strong></div>
    <div class="profile-stat"><span>Scans</span><strong>${profile.scanCount}</strong></div>
    <div class="profile-stat"><span>Pickups</span><strong>${profile.pickupCount}</strong></div>`;
  } catch {
    root.querySelector("[data-profile-summary]").textContent = "We could not load your profile right now. Please refresh and try again.";
  }
}

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
  try {
    const data = await fetchJson("/api/dashboard");
    root.querySelector("[data-dashboard-metrics]").innerHTML = data.metrics.map(metricCard).join("");
    root.querySelector("[data-dashboard-activity]").innerHTML = data.activity.map(item => `<article class="activity-item"><span class="${toneClass(item.tone)}">${item.date}</span><strong>${item.title}</strong><p>${item.detail}</p></article>`).join("");
    root.querySelector("[data-dashboard-pickups]").innerHTML = data.pickups.map(pickupMini).join("");
  } catch {
    root.querySelector("[data-dashboard-metrics]").innerHTML = '<p class="empty-state">Dashboard data is unavailable. Please refresh to try again.</p>';
  }
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

    const submit = form.querySelector('button[type="submit"]');
    if (submit) { submit.disabled = true; submit.classList.add("is-loading"); }
    status.textContent = "Classifying your upload…";
    status.classList.add("is-loading");
    try {
      const payload = await fetchJson("/api/scan/classify", { method: "POST", body: new FormData(form) });
      status.textContent = "Scan saved successfully.";
      status.classList.remove("is-loading");
      status.classList.add("is-success");
      result.hidden = false;
      result.innerHTML = `<div class="scan-result__media">${input.files[0].name}</div><div class="scan-result__details"><h2>${payload.item}</h2><div class="badge-row"><span class="reloop-badge reloop-badge--success">${payload.disposition}</span><span class="reloop-badge reloop-badge--warning">+${payload.points} pts</span><span class="reloop-badge reloop-badge--info">${payload.confidence}% confidence</span></div><dl><dt>Category</dt><dd>${payload.category}</dd><dt>Estimated Weight</dt><dd>${payload.estimatedWeight}</dd><dt>Date Scanned</dt><dd>${new Date(payload.dateScanned).toLocaleString()}</dd></dl><div class="button-pair"><a class="reloop-btn reloop-btn--primary" href="/Dashboard">View dashboard</a><button class="reloop-btn reloop-btn--secondary" type="button" data-scan-again>Scan another item</button></div></div>`;
      result.querySelector("[data-scan-again]").addEventListener("click", () => window.location.reload());
    } catch (error) {
      status.textContent = error.message || "We could not classify this image. Please try again.";
      status.classList.remove("is-loading");
    } finally {
      if (submit) { submit.disabled = false; submit.classList.remove("is-loading"); }
    }
  });
});

async function loadPickups(status = "All") {
  const root = document.querySelector(".js-pickups");
  if (!root) return;
  try {
    const rows = await fetchJson(`/api/pickups?status=${encodeURIComponent(status)}`);
    root.querySelector("[data-pickups-table]").innerHTML = rows.map(row => `<tr><td><strong>${row.id}</strong></td><td>${row.date}</td><td>${row.address}</td><td><strong>${row.wasteType}</strong></td><td>${row.weight}</td><td><span class="${toneClass(statusTone(row.status))}">${row.status}</span></td></tr>`).join("");
    const empty = root.querySelector("[data-pickups-empty]");
    empty.hidden = rows.length > 0;
    if (!rows.length) empty.textContent = "No pickups match this filter yet.";
  } catch {
    const empty = root.querySelector("[data-pickups-empty]");
    empty.textContent = "We could not load pickups. Please refresh and try again.";
    empty.hidden = false;
  }
}

document.querySelectorAll(".js-pickups .tab").forEach((button) => {
  button.addEventListener("click", () => {
    document.querySelectorAll(".js-pickups .tab").forEach((tab) => tab.classList.remove("is-active"));
    button.classList.add("is-active");
    loadPickups(button.dataset.status);
  });
});

loadDashboard();
loadPickups();
loadProfile();

const adminRoot = document.querySelector(".js-admin-management");
if (adminRoot) {
  const adminState = { users: 1, pickups: 1, activity: 1, redemptions: 1, userSearch: "", pickupSearch: "", pickupStatus: "All" };
  const dialog = adminRoot.parentElement.querySelector("[data-admin-dialog]");
  const fields = dialog.querySelector("[data-admin-editor-fields]");
  const editor = dialog.querySelector("[data-admin-editor]");
  const editorStatus = dialog.querySelector("[data-admin-editor-status]");
  const status = adminRoot.querySelector("[data-admin-status]");
  const escape = value => String(value ?? "").replace(/[&<>'"]/g, char => ({ "&":"&amp;", "<":"&lt;", ">":"&gt;", "'":"&#39;", '"':"&quot;" })[char]);
  const setStatus = message => { status.textContent = message; status.classList.toggle("is-success", Boolean(message)); };
  const pageButtons = (target, data, callback) => { target.innerHTML = `<span>${data.totalCount} total</span><button ${data.page === 1 ? "disabled" : ""}>Previous</button><span>Page ${data.page}</span><button ${data.page * data.pageSize >= data.totalCount ? "disabled" : ""}>Next</button>`; const buttons = target.querySelectorAll("button"); buttons[0].onclick = () => callback(data.page - 1); buttons[1].onclick = () => callback(data.page + 1); };
  async function loadOverview() { const data = await fetchJson("/api/admin/overview"); adminRoot.querySelector("[data-admin-metrics]").innerHTML = data.metrics.map(metricCard).join(""); }
  async function loadUsers(page = adminState.users) { adminState.users = page; const data = await fetchJson(`/api/admin/users?page=${page}&search=${encodeURIComponent(adminState.userSearch)}`); adminRoot.querySelector("[data-admin-users]").innerHTML = data.items.length ? data.items.map(user => `<tr><td><strong>${escape(user.fullName)}</strong><br><small>${escape(user.email)} · Joined ${escape(user.joined)}</small><br><small>${escape(user.address)} · ${escape(user.preferredCategory)}</small></td><td>${escape(user.role)}</td><td>${user.rewardPoints}</td><td>${user.pickups}</td><td><span class="${toneClass(user.isActive ? "success" : "danger")}">${user.isActive ? "Active" : "Inactive"}</span></td><td><button class="admin-action" data-edit-user="${encodeURIComponent(JSON.stringify(user))}">Edit</button></td></tr>`).join("") : '<tr><td colspan="6" class="empty-state">No members match that search.</td></tr>'; pageButtons(adminRoot.querySelector("[data-admin-users-pagination]"), data, loadUsers); adminRoot.querySelectorAll("[data-edit-user]").forEach(button => button.onclick = () => editUser(JSON.parse(decodeURIComponent(button.dataset.editUser)))); }
  async function loadPickups(page = adminState.pickups) { adminState.pickups = page; const data = await fetchJson(`/api/admin/pickups?page=${page}&status=${encodeURIComponent(adminState.pickupStatus)}&search=${encodeURIComponent(adminState.pickupSearch)}`); adminRoot.querySelector("[data-admin-pickups]").innerHTML = data.items.length ? data.items.map(item => `<tr><td><strong>${escape(item.reference)}</strong></td><td>${escape(item.member)}</td><td>${escape(item.date)}</td><td>${escape(item.material)}</td><td>${escape(item.assignedAgent || "Unassigned")}</td><td><span class="${toneClass(statusTone(item.status))}">${escape(item.status)}</span></td><td><button class="admin-action" data-edit-pickup="${encodeURIComponent(JSON.stringify(item))}">Manage</button></td></tr>`).join("") : '<tr><td colspan="7" class="empty-state">No pickups match these filters.</td></tr>'; pageButtons(adminRoot.querySelector("[data-admin-pickups-pagination]"), data, loadPickups); adminRoot.querySelectorAll("[data-edit-pickup]").forEach(button => button.onclick = () => editPickup(JSON.parse(decodeURIComponent(button.dataset.editPickup)))); }
  async function loadActivity(page = adminState.activity) { adminState.activity = page; const data = await fetchJson(`/api/admin/activity?page=${page}`); adminRoot.querySelector("[data-admin-activity]").innerHTML = data.items.length ? data.items.map(item => `<tr><td>${escape(item.member)}</td><td><strong>${escape(item.title)}</strong></td><td>${escape(item.detail)}</td><td>${escape(item.date)}</td></tr>`).join("") : '<tr><td colspan="4" class="empty-state">No recycling activity recorded.</td></tr>'; pageButtons(adminRoot.querySelector("[data-admin-activity-pagination]"), data, loadActivity); }
  async function loadRedemptions(page = adminState.redemptions) { adminState.redemptions = page; const data = await fetchJson(`/api/admin/redemptions?page=${page}`); adminRoot.querySelector("[data-admin-redemptions]").innerHTML = data.items.length ? data.items.map(item => `<tr><td>${escape(item.member)}</td><td>${escape(item.reward)}</td><td>${item.pointsSpent}</td><td>${escape(item.date)}</td><td>${escape(item.status)}</td><td><button class="admin-action" data-edit-redemption="${encodeURIComponent(JSON.stringify(item))}">Update</button></td></tr>`).join("") : '<tr><td colspan="6" class="empty-state">No redemption requests yet.</td></tr>'; pageButtons(adminRoot.querySelector("[data-admin-redemptions-pagination]"), data, loadRedemptions); adminRoot.querySelectorAll("[data-edit-redemption]").forEach(button => button.onclick = () => editRedemption(JSON.parse(decodeURIComponent(button.dataset.editRedemption)))); }
  async function loadRewards() { const rewards = await fetchJson("/api/admin/rewards"); adminRoot.querySelector("[data-admin-rewards]").innerHTML = rewards.length ? rewards.map(item => `<tr><td><strong>${escape(item.name)}</strong><br><small>${escape(item.description)}</small></td><td>${item.pointsCost} pts</td><td>${item.isActive ? "Active" : "Hidden"}</td><td><button class="admin-action" data-edit-reward="${encodeURIComponent(JSON.stringify(item))}">Edit</button></td></tr>`).join("") : '<tr><td colspan="4" class="empty-state">No rewards in the catalogue.</td></tr>'; adminRoot.querySelectorAll("[data-edit-reward]").forEach(button => button.onclick = () => editReward(JSON.parse(decodeURIComponent(button.dataset.editReward)))); }
  function openEditor(html, submit) { fields.innerHTML = html; editor.onsubmit = async event => { event.preventDefault(); const saveButton = editor.querySelector('button[type="submit"]'); if (saveButton.disabled) return; saveButton.disabled = true; editorStatus.textContent = "Saving…"; try { await submit(new FormData(editor)); dialog.close(); setStatus("Changes saved."); await refresh(); } catch (error) { editorStatus.textContent = error.message; } finally { saveButton.disabled = false; } }; editorStatus.textContent = ""; dialog.showModal(); }
  function editUser(user) { openEditor(`<h2>Edit member</h2><p>${escape(user.email)} · Joined ${escape(user.joined)}</p><label>Name<input name="fullName" value="${escape(user.fullName)}" required maxlength="120"></label><label>Address<input name="address" value="${escape(user.address)}" required maxlength="240"></label><label>Preferred material<input name="preferredCategory" value="${escape(user.preferredCategory)}" required maxlength="80"></label><label>Role<select name="role"><option ${user.role === "Member" ? "selected" : ""}>Member</option><option ${user.role === "Admin" ? "selected" : ""}>Admin</option></select></label><label><input name="isActive" type="checkbox" ${user.isActive ? "checked" : ""}> Account is active</label>`, data => fetchJson(`/api/admin/users/${user.id}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify({ fullName:data.get("fullName"), address:data.get("address"), preferredCategory:data.get("preferredCategory"), role:data.get("role"), isActive:data.get("isActive") === "on" }) })); }
  function editPickup(item) { openEditor(`<h2>Manage ${escape(item.reference)}</h2><label>Recycling agent<input name="assignedAgent" value="${escape(item.assignedAgent || "")}" placeholder="Agent name"></label><label>Status<select name="status">${["Scheduled","Assigned","Completed","Cancelled"].map(value => `<option ${item.status === value ? "selected" : ""}>${value}</option>`).join("")}</select></label>`, data => fetchJson(`/api/admin/pickups/${item.id}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify({ status:data.get("status"), assignedAgent:data.get("assignedAgent") }) })); }
  function editReward(item) { openEditor(`<h2>${item ? "Edit reward" : "New reward"}</h2><label>Name<input name="name" value="${escape(item?.name || "")}" required></label><label>Description<textarea name="description" required>${escape(item?.description || "")}</textarea></label><label>Points cost<input name="pointsCost" type="number" min="1" value="${item?.pointsCost || 100}" required></label><label><input name="isActive" type="checkbox" ${!item || item.isActive ? "checked" : ""}> Available to members</label>`, data => fetchJson(item ? `/api/admin/rewards/${item.id}` : "/api/admin/rewards", { method:item ? "PUT" : "POST", headers:{"Content-Type":"application/json"}, body:JSON.stringify({ name:data.get("name"), description:data.get("description"), pointsCost:Number(data.get("pointsCost")), isActive:data.get("isActive") === "on" }) })); }
  function editRedemption(item) { openEditor(`<h2>Update redemption</h2><p>${escape(item.member)} — ${escape(item.reward)}</p><label>Status<select name="status">${["Pending","Approved","Fulfilled","Declined"].map(value => `<option ${item.status === value ? "selected" : ""}>${value}</option>`).join("")}</select></label>`, data => fetchJson(`/api/admin/redemptions/${item.id}`, { method:"PUT", headers:{"Content-Type":"application/json"}, body:JSON.stringify({ status:data.get("status") }) })); }
  async function refresh() { try { await Promise.all([loadOverview(), loadUsers(), loadPickups(), loadActivity(), loadRewards(), loadRedemptions()]); } catch (error) { setStatus(error.message || "Could not load administration data."); } }
  let searchTimer; adminRoot.querySelector("[data-admin-user-search]").oninput = event => { clearTimeout(searchTimer); searchTimer = setTimeout(() => { adminState.userSearch = event.target.value; loadUsers(1); }, 250); }; adminRoot.querySelector("[data-admin-pickup-search]").oninput = event => { clearTimeout(searchTimer); searchTimer = setTimeout(() => { adminState.pickupSearch = event.target.value; loadPickups(1); }, 250); }; adminRoot.querySelector("[data-admin-pickup-status]").onchange = event => { adminState.pickupStatus = event.target.value; loadPickups(1); }; adminRoot.querySelector("[data-admin-new-reward]").onclick = () => editReward(); dialog.querySelector("[data-admin-cancel]").onclick = () => dialog.close(); refresh();
}
