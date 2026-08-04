import { describe, expect, it } from "vitest";
import { permissionsForRole } from "./permissions";

describe("tenant permissions", () => {
  it("derives the permission set from the active membership role", () => {
    const permissions = permissionsForRole("Manager");

    expect(permissions.has("MembersInvite")).toBe(true);
    expect(permissions.has("MembersManageRoles")).toBe(false);
  });

  it("returns no permissions without an active membership", () => {
    expect(permissionsForRole(undefined).size).toBe(0);
  });
});
