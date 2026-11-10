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
