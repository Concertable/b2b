import { create } from "zustand";
import { filterMembershipsByTenantType } from "../memberships";
import type { Membership, TenantType } from "../types";

export interface TenantStoreState {
  readonly activeTenantId: string | undefined;
  readonly isSelectionPending: boolean;
  readonly hydrateTenant: (tenantId: string | undefined) => void;
  readonly selectTenant: (tenantId: string) => void;
  readonly beginSelection: () => void;
  readonly endSelection: () => void;
  readonly clearTenant: () => void;
  readonly synchronizeTenant: (
    memberships: ReadonlyArray<Membership>,
    tenantType?: TenantType,
  ) => string | undefined;
}

export const useTenantStore = create<TenantStoreState>()((set, get) => ({
  activeTenantId: undefined,
  isSelectionPending: false,
  hydrateTenant: (activeTenantId) => set({ activeTenantId }),
  selectTenant: (activeTenantId) => set({ activeTenantId }),
  beginSelection: () => set({ isSelectionPending: true }),
  endSelection: () => set({ isSelectionPending: false }),
  clearTenant: () =>
    set({ activeTenantId: undefined, isSelectionPending: false }),
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
