import type { StoreApi } from "zustand/vanilla";
import { meQueryKey } from "@/features/user/hooks/useSyncUser";
import { queryClient } from "@/lib/queryClient";
import { resolveTenant } from "./memberships";
import {
  tenantStore,
  type TenantStoreState,
} from "./store/tenantStore";
import type { B2bIdentity, Membership, TenantType } from "./types";

interface TenantSessionDependencies {
  readonly store: StoreApi<TenantStoreState>;
  readonly memberships: () => ReadonlyArray<Membership>;
  readonly clearMemberships: () => void;
}

export function createTenantSession({
  store,
  memberships,
  clearMemberships,
}: TenantSessionDependencies) {
  return {
    tenantIdForRequest: () => {
      const activeTenantId = store.getState().activeTenantId;
      return memberships().some(
        (membership) => membership.tenantId === activeTenantId,
      )
        ? activeTenantId
        : undefined;
    },
    select: (tenantId: string) => store.getState().selectTenant(tenantId),
    clear: () => {
      store.getState().clearTenant();
      clearMemberships();
    },
    resolve: (tenantType: TenantType) => {
      const currentMemberships = memberships();
      store.getState().synchronizeTenant(currentMemberships, tenantType);
      return resolveTenant(
        currentMemberships,
        tenantType,
        store.getState().activeTenantId,
      );
    },
  };
}

const EMPTY_MEMBERSHIPS: ReadonlyArray<Membership> = [];

export const tenantSession = createTenantSession({
  store: tenantStore,
  memberships: () =>
    queryClient.getQueryData<B2bIdentity>(meQueryKey)?.memberships ??
    EMPTY_MEMBERSHIPS,
  clearMemberships: () => queryClient.removeQueries({ queryKey: meQueryKey }),
});
