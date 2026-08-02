import { describe, expect, it, vi } from "vitest";

vi.mock("@/features/user/hooks/useSyncUser", () => ({
  meQueryKey: ["auth", "me"],
}));
vi.mock("@/lib/queryClient", () => ({
  queryClient: { getQueryData: vi.fn() },
}));

import { createTenantStore } from "./store/tenantStore";
import { createTenantSession } from "./tenantSession";
import type { Membership } from "./types";

const venueMemberships: ReadonlyArray<Membership> = [
  {
    tenantId: "venue-one",
    legalName: "Venue One",
    type: "Venue",
    role: "Owner",
  },
  {
    tenantId: "venue-two",
    legalName: "Venue Two",
    type: "Venue",
    role: "Staff",
  },
];

function createSession(memberships: ReadonlyArray<Membership>) {
  const store = createTenantStore();
  const clearMemberships = vi.fn();
  return {
    store,
    clearMemberships,
    session: createTenantSession({
      store,
      memberships: () => memberships,
      clearMemberships,
    }),
  };
}

describe("tenant session", () => {
  it("selects a tenant for request-header resolution", () => {
    const { session } = createSession(venueMemberships);

    session.select("venue-two");

    expect(session.tenantIdForRequest()).toBe("venue-two");
    expect(session.resolve("Venue").activeMembership).toEqual(
      venueMemberships[1],
    );
  });

  it("reconciles a stale selection before resolving a route", () => {
    const { session } = createSession(venueMemberships);
    session.select("removed-venue");

    const resolution = session.resolve("Venue");

    expect(session.tenantIdForRequest()).toBeUndefined();
    expect(resolution.selectionRequired).toBe(true);
  });

  it("does not resolve a stale request header before identity reconciliation", () => {
    const { session } = createSession(venueMemberships);
    session.select("removed-venue");
    expect(session.tenantIdForRequest()).toBeUndefined();
  });

  it("selects a sole membership while resolving its route", () => {
    const store = createTenantStore();
    const singleMembership = venueMemberships.slice(0, 1);
    const session = createTenantSession({
      store,
      memberships: () => singleMembership,
      clearMemberships: vi.fn(),
    });

    const resolution = session.resolve("Venue");

    expect(resolution.activeMembership).toEqual(singleMembership[0]);
    expect(session.tenantIdForRequest()).toBe("venue-one");
  });

  it("clears the tenant session on logout", () => {
    const { session, clearMemberships } = createSession(venueMemberships);
    session.select("venue-one");

    session.clear();

    expect(session.tenantIdForRequest()).toBeUndefined();
    expect(clearMemberships).toHaveBeenCalledOnce();
  });
});
