import { beforeEach, describe, expect, it, vi } from "vitest";
import { createTenantSession } from "./tenantSession";
import { useTenantStore } from "./store/useTenantStore";
import type { Membership, TenantStorage } from "./types";

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

  it("serializes concurrent selections and commits only the latest tenant", async () => {
    let releaseFirstSave: (() => void) | undefined;
    const firstSave = new Promise<void>((resolve) => {
      releaseFirstSave = resolve;
    });
    const { session, storage } = await createSession(venueMemberships);
    vi.mocked(storage.saveActiveTenantId)
      .mockImplementationOnce(() => firstSave)
      .mockResolvedValueOnce(undefined);

    const firstSelection = session.select("venue-one");
    await vi.waitFor(() =>
      expect(storage.saveActiveTenantId).toHaveBeenCalledWith("venue-one"),
    );
    const latestSelection = session.select("venue-two");

    expect(useTenantStore.getState().isSelectionPending).toBe(true);
    expect(session.tenantIdForRequest()).toBeUndefined();
    releaseFirstSave?.();
    await Promise.all([firstSelection, latestSelection]);

    expect(storage.saveActiveTenantId).toHaveBeenNthCalledWith(1, "venue-one");
    expect(storage.saveActiveTenantId).toHaveBeenNthCalledWith(2, "venue-two");
    expect(session.tenantIdForRequest()).toBe("venue-two");
    expect(useTenantStore.getState().isSelectionPending).toBe(false);
  });

  it("suppresses an older failure after a newer selection succeeds", async () => {
    let rejectFirstSave: ((error: Error) => void) | undefined;
    const firstSave = new Promise<void>((_, reject) => {
      rejectFirstSave = reject;
    });
    const { session, storage } = await createSession(venueMemberships);
    vi.mocked(storage.saveActiveTenantId)
      .mockImplementationOnce(() => firstSave)
      .mockResolvedValueOnce(undefined);

    const olderSelection = session.select("venue-one");
    await vi.waitFor(() =>
      expect(storage.saveActiveTenantId).toHaveBeenCalledWith("venue-one"),
    );
    const latestSelection = session.select("venue-two");
    rejectFirstSave?.(new Error("stale write failed"));

    await expect(olderSelection).resolves.toBeUndefined();
    await expect(latestSelection).resolves.toBeUndefined();
    expect(session.tenantIdForRequest()).toBe("venue-two");
    expect(useTenantStore.getState().isSelectionPending).toBe(false);
  });

  it("recovers from a failed hydration when configuration is retried", async () => {
    const storage = createStorage("venue-one");
    vi.mocked(storage.loadActiveTenantId)
      .mockRejectedValueOnce(new Error("SecureStore unavailable"))
      .mockResolvedValueOnce("venue-one");
    const session = createTenantSession(useTenantStore);
    const configuration = {
      storage,
      memberships: () => venueMemberships,
      clearMemberships: vi.fn(),
    };

    await expect(session.configure(configuration)).rejects.toThrow(
      "SecureStore unavailable",
    );
    await expect(session.configure(configuration)).resolves.toBeUndefined();

    expect(session.tenantIdForRequest()).toBe("venue-one");
  });

  it("persists a sole membership while resolving a route", async () => {
    const { session, storage } = await createSession(
      venueMemberships.slice(0, 1),
    );

    const resolution = await session.resolve("venue");

    expect(resolution.activeMembership).toEqual(venueMemberships[0]);
    expect(storage.saveActiveTenantId).toHaveBeenCalledWith("venue-one");
  });

  it("selects across all membership types for a cross-platform B2B app", async () => {
    const memberships: ReadonlyArray<Membership> = [
      ...venueMemberships,
      {
        tenantId: "artist-one",
        legalName: "Artist One",
        type: "artist",
        role: "manager",
      },
    ];
    const { session } = await createSession(memberships);

    await session.select("artist-one");

    expect((await session.resolve()).activeMembership).toEqual(memberships[2]);
    expect(session.tenantIdForRequest()).toBe("artist-one");
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
