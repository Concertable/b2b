import { beforeEach, describe, expect, it, vi } from "vitest";
import { createTenantSession } from "./tenantSession";
import { useTenantStore } from "./store/useTenantStore";
import type { Membership, TenantStorage } from "./types";

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

function createStorage(activeTenantId?: string) {
  const storage: TenantStorage = {
    loadActiveTenantId: vi.fn(() => activeTenantId),
    saveActiveTenantId: vi.fn((tenantId) => {
      activeTenantId = tenantId;
    }),
    clearActiveTenantId: vi.fn(() => {
      activeTenantId = undefined;
    }),
  };
  return storage;
}

async function createSession(
  memberships: ReadonlyArray<Membership>,
  activeTenantId?: string,
) {
  const clearMemberships = vi.fn();
  const storage = createStorage(activeTenantId);
  const session = createTenantSession(useTenantStore);
  await session.configure({
    storage,
    memberships: () => memberships,
    clearMemberships,
  });
  return { clearMemberships, session, storage };
}

describe("tenant session", () => {
  beforeEach(() => useTenantStore.getState().clearTenant());

  it("hydrates and validates the request tenant against current memberships", async () => {
    const { session } = await createSession(venueMemberships, "venue-two");

    expect(session.tenantIdForRequest()).toBe("venue-two");
  });

  it("rejects a tenant outside the current memberships", async () => {
    const { session } = await createSession(venueMemberships);

    await expect(session.select("removed-venue")).rejects.toBeInstanceOf(
      RangeError,
    );
    expect(session.tenantIdForRequest()).toBeUndefined();
  });

  it("persists an explicit selection", async () => {
    const { session, storage } = await createSession(venueMemberships);

    await session.select("venue-two");

    expect(session.tenantIdForRequest()).toBe("venue-two");
    expect(storage.saveActiveTenantId).toHaveBeenCalledWith("venue-two");
  });

  it("persists a sole membership while resolving a route", async () => {
    const { session, storage } = await createSession(
      venueMemberships.slice(0, 1),
    );

    const resolution = await session.resolve("Venue");

    expect(resolution.activeMembership).toEqual(venueMemberships[0]);
    expect(storage.saveActiveTenantId).toHaveBeenCalledWith("venue-one");
  });

  it("clears persisted selection and memberships on logout", async () => {
    const { clearMemberships, session, storage } = await createSession(
      venueMemberships,
      "venue-one",
    );

    await session.clear();

    expect(session.tenantIdForRequest()).toBeUndefined();
    expect(storage.clearActiveTenantId).toHaveBeenCalledOnce();
    expect(clearMemberships).toHaveBeenCalledOnce();
  });
});
