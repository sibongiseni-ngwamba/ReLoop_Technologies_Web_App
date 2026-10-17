const authForms = [...document.querySelectorAll(".js-firebase-auth")];
const logoutButtons = [...document.querySelectorAll(".js-logout")];
const passwordResetButtons = [...document.querySelectorAll("[data-password-reset]")];
if (!authForms.length && !logoutButtons.length && !passwordResetButtons.length) {
  // This page does not need the Firebase client SDK.
} else {
  const { initializeApp } = await import("https://www.gstatic.com/firebasejs/13.0.0/firebase-app.js");
  const { getAuth, setPersistence, browserLocalPersistence, browserSessionPersistence, createUserWithEmailAndPassword, signInWithEmailAndPassword, sendPasswordResetEmail, signOut } =
    await import("https://www.gstatic.com/firebasejs/13.0.0/firebase-auth.js");

  let auth;
  async function getFirebaseAuth() {
    if (auth) return auth;
    const response = await fetch("/api/firebase/config", { headers: { Accept: "application/json" } });
    const config = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(config.detail || config.title || "Firebase sign-in is not configured yet.");
    auth = getAuth(initializeApp(config));
    return auth;
  }

  function showStatus(element, message, success = false) {
    element.textContent = message;
    element.classList.toggle("is-success", success);
    element.classList.remove("is-loading");
  }

  const errorMessages = {
    "auth/invalid-credential": "Email or password is incorrect.",
    "auth/email-already-in-use": "An account already exists for this email address.",
    "auth/weak-password": "Choose a stronger password (at least 6 characters).",
    "auth/invalid-email": "Enter a valid email address.",
    "auth/too-many-requests": "Too many attempts. Wait a moment and try again.",
    "auth/network-request-failed": "Firebase could not connect. Check your internet connection."
  };

  passwordResetButtons.forEach((button) => button.addEventListener("click", async () => {
    const form = button.closest("form");
    const email = form?.querySelector('[name="email"]');
    const status = form?.querySelector(".form-status");
    if (!email || !status || !email.reportValidity()) return;

    button.disabled = true;
    status.textContent = "Requesting a password reset…";
    status.classList.add("is-loading");
    try {
      const firebaseAuth = await getFirebaseAuth();
      await sendPasswordResetEmail(firebaseAuth, email.value.trim());
      showStatus(status, "If an account exists for that email, a password reset message will be sent.", true);
    } catch (error) {
      if (error.code === "auth/user-not-found" || error.code === "auth/invalid-credential") {
        showStatus(status, "If an account exists for that email, a password reset message will be sent.", true);
      } else {
        showStatus(status, errorMessages[error.code] || "Could not send a password reset email. Please try again.");
      }
    } finally {
      button.disabled = false;
    }
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
      const firebaseAuth = await getFirebaseAuth();
      await setPersistence(firebaseAuth, values.keepSignedIn === "true" ? browserLocalPersistence : browserSessionPersistence);
      const credential = form.dataset.mode === "signup"
        ? await createUserWithEmailAndPassword(firebaseAuth, values.email.trim(), values.password)
        : await signInWithEmailAndPassword(firebaseAuth, values.email.trim(), values.password);
      const idToken = await credential.user.getIdToken();
      const endpoint = form.dataset.mode === "signup" ? "/api/auth/signup" : "/api/auth/login";
      const payload = form.dataset.mode === "signup"
        ? { idToken, fullName: values.fullName.trim() }
        : { idToken, keepSignedIn: values.keepSignedIn === "true" };
      const response = await fetch(endpoint, {
        method: "POST", headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify(payload)
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) throw new Error(result.detail || result.title || "Could not start your ReLoop session.");
      await signOut(firebaseAuth);
      showStatus(status, "Signed in successfully. Redirecting…", true);
      window.setTimeout(() => window.location.assign(form.dataset.redirect || "/Dashboard"), 300);
    } catch (error) {
      const message = errorMessages[error.code] || error.message || "Sign-in failed. Please try again.";
      showStatus(status, message);
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
      const firebaseAuth = await getFirebaseAuth();
      await signOut(firebaseAuth);
      showStatus(status, "You are logged out. Redirecting to the home page…", true);
      window.setTimeout(() => window.location.assign("/"), 400);
    } catch {
      showStatus(status, "Could not contact the server. Please try again.");
      button.disabled = false;
    }
  }));
}
