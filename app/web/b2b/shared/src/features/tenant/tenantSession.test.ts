import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@concertable/web/features/user/hooks/useSyncUser", () => ({
  meQueryKey: ["auth", "me"],
}));
vi.mock("@concertable/web/lib/queryClient", () => ({
  queryClient: { getQueryData: vi.fn() },
}));

import { useTenantStore } from "./store/useTenantStore";
import { createTenantSession } from "./tenantSession";
import type { Membership } from "./types";

const venueMemberships: ReadonlyArray<Membership> = [
  {
    tenantId: "venue-one",
    legalName: "Venue One",
    type: "venue",
    role: "owner",
  },
  {
    tenantId: "venue-two",
    legalName: "Venue Two",
    type: "venue",
    role: "staff",
  },
];

function createSession(memberships: ReadonlyArray<Membership>) {
  const clearMemberships = vi.fn();
  return {
    clearMemberships,
    session: createTenantSession({
      store: useTenantStore,
      memberships: () => memberships,
      clearMemberships,
    }),
  };
}

describe("tenant session", () => {
  beforeEach(() => useTenantStore.getState().clearTenant());

  it("selects a tenant for request-header resolution", () => {
    const { session } = createSession(venueMemberships);

    session.select("venue-two");

    expect(session.tenantIdForRequest()).toBe("venue-two");
    expect(session.resolve("venue").activeMembership).toEqual(
      venueMemberships[1],
    );
  });

  it("reconciles a stale selection before resolving a route", () => {
    const { session } = createSession(venueMemberships);
    session.select("removed-venue");

    const resolution = session.resolve("venue");

    expect(session.tenantIdForRequest()).toBeUndefined();
    expect(resolution.selectionRequired).toBe(true);
  });

  it("does not resolve a stale request header before identity reconciliation", () => {
    const { session } = createSession(venueMemberships);
    session.select("removed-venue");
    expect(session.tenantIdForRequest()).toBeUndefined();
  });

  it("selects a sole membership while resolving its route", () => {
    const singleMembership = venueMemberships.slice(0, 1);
    const session = createTenantSession({
      store: useTenantStore,
      memberships: () => singleMembership,
      clearMemberships: vi.fn(),
    });

    const resolution = session.resolve("venue");

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
