import { afterEach, describe, expect, it, vi } from "vitest";
import { useTenantStore } from "../store/useTenantStore";
import { tenantSession } from "../tenantSession";
import type { Membership, TenantStorage } from "../types";
import { useTenant } from "./useTenant";

vi.mock("react", () => ({
  useDebugValue: () => undefined,
  useEffect: (effect: () => void) => effect(),
  useMemo: <T>(factory: () => T) => factory(),
  useSyncExternalStore: (
    _subscribe: unknown,
    getSnapshot: () => unknown,
  ) => getSnapshot(),
}));
vi.mock("zustand", async () => {
  const { createStore } = await vi.importActual<
    typeof import("zustand/vanilla")
  >("zustand/vanilla");
  return {
    create:
      <T,>() =>
      (initializer: Parameters<typeof createStore<T>>[0]) => {
        const store = createStore<T>()(initializer);
        const useStore = <U,>(selector: (state: T) => U) =>
          selector(store.getState());
        return Object.assign(useStore, store);
      },
  };
});

const membership: Membership = {
  membershipId: "membership-one",
  tenantId: "tenant-one",
  legalName: "Tenant One",
  businessActivities: ["venueOperator"],
  role: "owner",
  permissionVersion: 1,
  permissions: ["tenant.settings.edit"],
};

describe("useTenant", () => {
  afterEach(async () => {
    await tenantSession.clear();
    useTenantStore.getState().clearTenant();
  });

  it("clears the final tenant from the store, session, and persistence", async () => {
    let memberships: ReadonlyArray<Membership> = [membership];
    let persistedTenantId: string | undefined;
    const storage: TenantStorage = {
      loadActiveTenantId: vi.fn(() => persistedTenantId),
      saveActiveTenantId: vi.fn((tenantId) => {
        persistedTenantId = tenantId;
      }),
      clearActiveTenantId: vi.fn(() => {
        persistedTenantId = undefined;
      }),
    };
    await tenantSession.configure({
      storage,
      memberships: () => memberships,
      clearMemberships: vi.fn(),
    });

    useTenant(memberships, "venueOperator");
    await vi.waitFor(() =>
      expect(storage.saveActiveTenantId).toHaveBeenCalledWith("tenant-one"),
    );

    memberships = [];
    useTenant(memberships, "venueOperator");
    await vi.waitFor(() =>
      expect(storage.clearActiveTenantId).toHaveBeenCalledOnce(),
    );

    expect(useTenantStore.getState().activeTenantId).toBeUndefined();
    expect(tenantSession.tenantIdForRequest()).toBeUndefined();
    expect(persistedTenantId).toBeUndefined();
    expect(storage.saveActiveTenantId).toHaveBeenCalledOnce();
  });
});
