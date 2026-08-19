import type { StoreApi } from "zustand/vanilla";
import { resolveTenant } from "./memberships";
import {
  useTenantStore,
  type TenantStoreState,
} from "./store/useTenantStore";
import type {
  TenantSessionConfiguration,
  TenantStorage,
  TenantType,
} from "./types";

function requireConfiguration(
  configuration: TenantSessionConfiguration | undefined,
): TenantSessionConfiguration {
  if (configuration === undefined)
    throw new Error("The tenant session has not been configured.");
  return configuration;
}

async function persistSelection(
  storage: TenantStorage,
  tenantId: string | undefined,
): Promise<void> {
  if (tenantId === undefined) await storage.clearActiveTenantId();
  else await storage.saveActiveTenantId(tenantId);
}

export function createTenantSession(store: StoreApi<TenantStoreState>) {
  let configuration: TenantSessionConfiguration | undefined;

  return {
    configure: async (nextConfiguration: TenantSessionConfiguration) => {
      configuration = nextConfiguration;
      store
        .getState()
        .hydrateTenant(await nextConfiguration.storage.loadActiveTenantId());
    },
    tenantIdForRequest: () => {
      if (configuration === undefined) return undefined;
      const activeTenantId = store.getState().activeTenantId;
      return configuration
        .memberships()
        .some((membership) => membership.tenantId === activeTenantId)
        ? activeTenantId
        : undefined;
    },
    select: async (tenantId: string) => {
      const current = requireConfiguration(configuration);
      if (
        !current
          .memberships()
          .some((membership) => membership.tenantId === tenantId)
      )
        throw new RangeError(`Tenant ${tenantId} is not an active membership.`);
      store.getState().selectTenant(tenantId);
      await current.storage.saveActiveTenantId(tenantId);
    },
    clear: async () => {
      store.getState().clearTenant();
      if (configuration === undefined) return;
      await Promise.all([
        configuration.storage.clearActiveTenantId(),
        configuration.clearMemberships(),
      ]);
    },
    resolve: async (tenantType: TenantType) => {
      const current = requireConfiguration(configuration);
      const memberships = current.memberships();
      const previousTenantId = store.getState().activeTenantId;
      const activeTenantId = store
        .getState()
        .synchronizeTenant(memberships, tenantType);
      if (activeTenantId !== previousTenantId)
        await persistSelection(current.storage, activeTenantId);
      return resolveTenant(memberships, tenantType, activeTenantId);
    },
  };
}

export const tenantSession = createTenantSession(useTenantStore);
