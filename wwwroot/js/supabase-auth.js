const authForms = [...document.querySelectorAll(".js-supabase-auth")];
const logoutButtons = [...document.querySelectorAll(".js-logout")];
const passwordResetButtons = [...document.querySelectorAll("[data-password-reset]")];
let client;

async function getSupabase() {
  if (client) return client;
  const response = await fetch("/api/supabase/config", { headers: { Accept: "application/json" } });
  const config = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(config.detail || config.title || "Supabase sign-in is not configured yet.");
  const { createClient } = await import("https://esm.sh/@supabase/supabase-js@2");
  client = createClient(config.url, config.anonKey, { auth: { persistSession: false, autoRefreshToken: false } });
  return client;
}

function showStatus(element, message, success = false) {
  if (!element) return;
  element.textContent = message;
  element.classList.toggle("is-success", success);
  element.classList.remove("is-loading");
}

passwordResetButtons.forEach((button) => button.addEventListener("click", async () => {
  const form = button.closest("form");
  const email = form?.querySelector('[name="email"]');
  const status = form?.querySelector(".form-status");
  if (!email || !status || !email.reportValidity()) return;
  button.disabled = true;
  status.textContent = "Requesting a password reset…";
  status.classList.add("is-loading");
  try {
    const supabase = await getSupabase();
    const { error } = await supabase.auth.resetPasswordForEmail(email.value.trim());
    if (error) throw error;
    showStatus(status, "If an account exists for that email, a password reset message will be sent.", true);
  } catch (error) {
    showStatus(status, error.message || "Could not send a password reset email. Please try again.");
  } finally { button.disabled = false; }
}));

authForms.forEach((form) => form.addEventListener("submit", async (event) => {
  event.preventDefault();
  const status = form.querySelector(".form-status");
  const button = form.querySelector('button[type="submit"]');
  const initialLabel = button.innerHTML;
  const values = Object.fromEntries(new FormData(form));
  form.querySelectorAll("[data-error-for]").forEach((field) => field.textContent = "");
  if (!form.reportValidity()) return;
  if (form.dataset.mode === "signup" && values.password !== values.confirmPassword) {
    form.querySelector('[data-error-for="confirmPassword"]').textContent = "Passwords do not match.";
    showStatus(status, "Please correct the highlighted fields.");
    return;
  }
  button.disabled = true;
  button.classList.add("is-loading");
  button.textContent = form.dataset.mode === "signup" ? "Creating account…" : "Signing in…";
  status.textContent = "Connecting securely…";
  status.classList.add("is-loading");
  try {
    const supabase = await getSupabase();
    const result = form.dataset.mode === "signup"
      ? await supabase.auth.signUp({ email: values.email.trim(), password: values.password, options: { data: { full_name: values.fullName.trim(), first_name: values.fullName.trim().split(/\s+/)[0], last_name: values.fullName.trim().split(/\s+/).slice(1).join(" ") } } })
      : await supabase.auth.signInWithPassword({ email: values.email.trim(), password: values.password });
    if (result.error) throw result.error;
    const session = result.data.session;
    if (!session?.access_token) {
      showStatus(status, "Check your email to confirm your account, then sign in.", true);
      return;
    }
    const endpoint = form.dataset.mode === "signup" ? "/api/auth/signup" : "/api/auth/login";
    const payload = form.dataset.mode === "signup"
      ? { idToken: session.access_token, fullName: values.fullName.trim() }
      : { idToken: session.access_token, keepSignedIn: values.keepSignedIn === "true" };
    const response = await fetch(endpoint, {
      method: "POST", headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify(payload)
    });
    const resultBody = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(resultBody.detail || resultBody.title || "Could not start your ReLoop session.");
    await supabase.auth.signOut();
    showStatus(status, "Signed in successfully. Redirecting…", true);
    window.setTimeout(() => window.location.assign(form.dataset.redirect || "/Dashboard"), 300);
  } catch (error) {
    showStatus(status, error.message || "Sign-in failed. Please try again.");
    if (error.code?.includes("email")) form.querySelector('[name="email"]')?.focus();
  } finally {
    button.disabled = false;
    button.classList.remove("is-loading");
    button.innerHTML = initialLabel;
  }
}));

logoutButtons.forEach((button) => button.addEventListener("click", async () => {
  const status = button.parentElement.querySelector(".form-status");
  button.disabled = true;
  try {
    const response = await fetch("/api/auth/logout", { method: "POST" });
    if (!response.ok) throw new Error("Logout request failed.");
    showStatus(status, "You are logged out. Redirecting to the home page…", true);
    window.setTimeout(() => window.location.assign("/"), 400);
  } catch { showStatus(status, "Could not contact the server. Please try again."); button.disabled = false; }
}));



