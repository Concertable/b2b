import { create } from "zustand";
import { filterMembershipsByTenantType } from "../memberships";
import type { Membership, TenantType } from "../types";

export interface TenantStoreState {
  readonly activeTenantId: string | undefined;
  readonly hydrateTenant: (tenantId: string | undefined) => void;
  readonly selectTenant: (tenantId: string) => void;
  readonly clearTenant: () => void;
  readonly synchronizeTenant: (
    memberships: ReadonlyArray<Membership>,
    tenantType?: TenantType,
  ) => string | undefined;
}

export const useTenantStore = create<TenantStoreState>()((set, get) => ({
  activeTenantId: undefined,
  hydrateTenant: (activeTenantId) => set({ activeTenantId }),
  selectTenant: (activeTenantId) => set({ activeTenantId }),
  clearTenant: () => set({ activeTenantId: undefined }),
  synchronizeTenant: (memberships, tenantType) => {
    const matchingMemberships = filterMembershipsByTenantType(
      memberships,
      tenantType,
    );
    const activeTenantId = get().activeTenantId;
    const nextTenantId = matchingMemberships.some(
      (membership) => membership.tenantId === activeTenantId,
    )
      ? activeTenantId
      : matchingMemberships.length === 1
        ? matchingMemberships[0].tenantId
        : undefined;
    if (nextTenantId !== activeTenantId) set({ activeTenantId: nextTenantId });
    return nextTenantId;
  },
}));
