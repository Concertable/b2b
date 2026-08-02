import { create } from "zustand";
import { persist } from "zustand/middleware";

interface ActiveTenantState {
  activeTenantId: string | undefined;
  setActiveTenant: (tenantId: string | undefined) => void;
}

export const useActiveTenantStore = create<ActiveTenantState>()(
  persist(
    (set) => ({
      activeTenantId: undefined,
      setActiveTenant: (activeTenantId) => set({ activeTenantId }),
    }),
    { name: "concertable.active-tenant" },
  ),
);
