const test = require("node:test");
const assert = require("node:assert/strict");
const domain = require("../../wwwroot/js/reloop-domain.js");

test("admin role check matches mobile role keys case-insensitively", () => {
  assert.equal(domain.isAdminRole("admin"), true);
  assert.equal(domain.isAdminRole("ADMIN"), true);
  assert.equal(domain.isAdminRole("agent"), false);
  assert.equal(domain.isAdminRole(undefined), false);
});

test("name mapper splits the mobile firstName and lastName fields", () => {
  assert.deepEqual(domain.splitName("  Alex   Rivera "), { firstName: "Alex", lastName: "Rivera" });
  assert.deepEqual(domain.splitName("Madonna"), { firstName: "Madonna", lastName: "" });
  assert.deepEqual(domain.splitName(""), { firstName: "", lastName: "" });
});

test("Firebase startup requires a registered Web app config", () => {
  assert.equal(domain.isValidWebFirebaseConfig({ apiKey: "k", authDomain: "a", projectId: "p", appId: "i" }), true);
  assert.equal(domain.isValidWebFirebaseConfig({ apiKey: "android-key", projectId: "p" }), false);
});

test("page access separates guests, members, and administrators", () => {
  assert.equal(domain.accessForPath("/Dashboard", false, null), "sign-in");
  assert.equal(domain.accessForPath("/Admin/Index", false, null), "sign-in");
  assert.equal(domain.accessForPath("/Admin/Index", true, "resident"), "forbidden");
  assert.equal(domain.accessForPath("/Admin/Index", true, "admin"), "allow");
  assert.equal(domain.accessForPath("/ScanWaste", true, "agent"), "forbidden");
  assert.equal(domain.accessForPath("/MyPickups", true, "agent"), "allow");
  assert.equal(domain.accessForPath("/MyPickups", true, "admin"), "forbidden");
  assert.equal(domain.accessForPath("/Login", true, "resident"), "signed-in-only");
  assert.equal(domain.accessForPath("/About", false, null), "allow");
});

test("pickup agent actions follow the mobile status transitions", () => {
  assert.equal(domain.canTransitionPickup("Scheduled", "In Progress"), true);
  assert.equal(domain.canTransitionPickup("In Progress", "Completed"), true);
  assert.equal(domain.canTransitionPickup("Scheduled", "Completed"), false);
  assert.equal(domain.canTransitionPickup("Completed", "Scheduled"), false);
});
