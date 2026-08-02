import { createStore, type StateCreator } from "zustand/vanilla";
import { persist } from "zustand/middleware";
import { filterMembershipsByPersona } from "../memberships";
import type { Membership, TenantType } from "../types";

export interface TenantStoreState {
  readonly activeTenantId: string | undefined;
  readonly selectTenant: (tenantId: string) => void;
  readonly clearTenant: () => void;
  readonly synchronizeTenant: (
    memberships: ReadonlyArray<Membership>,
    persona: TenantType,
  ) => void;
}

const createTenantState: StateCreator<TenantStoreState> = (set) => ({
  activeTenantId: undefined,
  selectTenant: (activeTenantId) => set({ activeTenantId }),
  clearTenant: () => set({ activeTenantId: undefined }),
  synchronizeTenant: (memberships, persona) =>
    set((state) => {
      const matchingMemberships = filterMembershipsByPersona(
        memberships,
        persona,
      );
      if (
        matchingMemberships.some(
          (membership) => membership.tenantId === state.activeTenantId,
        )
      )
        return state;

      return {
        activeTenantId:
          matchingMemberships.length === 1
            ? matchingMemberships[0].tenantId
            : undefined,
      };
    }),
});

export function createTenantStore() {
  return createStore<TenantStoreState>()(createTenantState);
}

export const tenantStore = createStore<TenantStoreState>()(
  persist(createTenantState, { name: "concertable.active-tenant" }),
);
