import { create } from "zustand";
import { persist } from "zustand/middleware";
import { filterMembershipsByTenantType } from "../memberships";
import type { Membership, TenantType } from "../types";

export interface TenantStoreState {
  readonly activeTenantId: string | undefined;
  readonly selectTenant: (tenantId: string) => void;
  readonly clearTenant: () => void;
  readonly synchronizeTenant: (
    memberships: ReadonlyArray<Membership>,
    tenantType: TenantType,
  ) => void;
}

export const useTenantStore = create<TenantStoreState>()(
  persist(
    (set) => ({
      activeTenantId: undefined,
      selectTenant: (activeTenantId) => set({ activeTenantId }),
      clearTenant: () => set({ activeTenantId: undefined }),
      synchronizeTenant: (memberships, tenantType) =>
        set((state) => {
          const matchingMemberships = filterMembershipsByTenantType(
            memberships,
            tenantType,
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
    }),
    { name: "concertable.active-tenant" },
  ),
);
