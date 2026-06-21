import { create } from "zustand";
import { persist } from "zustand/middleware";

interface ActiveTenantState {
  /** The tenant the manager is acting as, sent as X-Tenant-Id. Null = no explicit selection. */
  activeTenantId: string | null;
  setActiveTenant: (tenantId: string | null) => void;
}

/**
 * Persists the selected tenant across reloads. Written by the tenant switcher (Phase 6) and read by the axios
 * interceptors (via installTenantHeaders) to stamp X-Tenant-Id. Until a user holds more than one membership
 * nothing selects a tenant, so the header is never sent and single-tenant behaviour is unchanged.
 */
export const useActiveTenantStore = create<ActiveTenantState>()(
  persist(
    (set) => ({
      activeTenantId: null,
      setActiveTenant: (activeTenantId) => set({ activeTenantId }),
    }),
    { name: "concertable.active-tenant" },
  ),
);
