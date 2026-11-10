((root, create) => {
  const domain = create();
  if (typeof module !== "undefined" && module.exports) module.exports = domain;
  else root.ReLoopDomain = domain;
})(globalThis, () => ({
  isAdminRole(role) {
    return String(role || "").trim().toLowerCase() === "admin";
  },
  splitName(fullName) {
    const [firstName = "", ...rest] = String(fullName || "").trim().split(/\s+/).filter(Boolean);
    return { firstName, lastName: rest.join(" ") };
  },
  isValidWebFirebaseConfig(config) {
    return Boolean(config?.apiKey && config?.authDomain && config?.projectId && config?.appId);
  },
  accessForPath(path, signedIn, role) {
    const normalized = String(path || "/").replace(/\/$/, "").toLowerCase() || "/";
    const normalizedRole = String(role || "").trim().toLowerCase();
    if (normalized === "/login" || normalized === "/signup") return signedIn ? "signed-in-only" : "allow";
    if (normalized === "/admin" || normalized === "/admin/index") return !signedIn ? "sign-in" : (this.isAdminRole(role) ? "allow" : "forbidden");
    const protectedRoutes = ["/dashboard", "/scanwaste", "/schedulepickup", "/mypickups", "/profile", "/logout"];
    if (protectedRoutes.includes(normalized) && !signedIn) return "sign-in";
    if (["/scanwaste", "/schedulepickup"].includes(normalized) && !["resident", "admin"].includes(normalizedRole)) return "forbidden";
    if (normalized === "/mypickups" && !["resident", "agent"].includes(normalizedRole)) return "forbidden";
    return "allow";
  },
  canTransitionPickup(from, to) {
    const current = String(from || "").trim().toLowerCase();
    const next = String(to || "").trim().toLowerCase();
    return (current === "scheduled" && ["in progress", "cancelled"].includes(next)) || (current === "in progress" && next === "completed");
  }
}));
