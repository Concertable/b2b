import {
  b2bIdentityKeys,
  tenantSession,
} from "@concertable/b2b/features/tenant";
import type { B2bIdentity, TenantStorage } from "@concertable/b2b/features/tenant/types";
import { queryClient } from "@concertable/web/lib/queryClient";

const ACTIVE_TENANT_KEY = "concertable.active-tenant";

interface PersistedTenantState {
  readonly state?: { readonly activeTenantId?: unknown };
}

const webTenantStorage: TenantStorage = {
  loadActiveTenantId: () => {
    const value = localStorage.getItem(ACTIVE_TENANT_KEY);
    if (value === null) return undefined;
    try {
      const persisted = JSON.parse(value) as PersistedTenantState;
      return typeof persisted.state?.activeTenantId === "string"
        ? persisted.state.activeTenantId
        : undefined;
    } catch {
      return undefined;
    }
  },
  saveActiveTenantId: (activeTenantId) =>
    localStorage.setItem(
      ACTIVE_TENANT_KEY,
      JSON.stringify({ state: { activeTenantId }, version: 0 }),
    ),
  clearActiveTenantId: () => localStorage.removeItem(ACTIVE_TENANT_KEY),
};

export const tenantSessionReady = tenantSession.configure({
  storage: webTenantStorage,
  memberships: () =>
    queryClient.getQueryData<B2bIdentity>(b2bIdentityKeys.all())?.memberships ??
    [],
  clearMemberships: () =>
    queryClient.removeQueries({ queryKey: b2bIdentityKeys.all() }),
});
