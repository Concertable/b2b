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
  let latestSelection = 0;
  let selectionQueue = Promise.resolve();

  const enqueue = (operation: () => Promise<void>) => {
    const queued = selectionQueue.catch(() => undefined).then(operation);
    selectionQueue = queued.catch(() => undefined);
    return queued;
  };

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
      const selection = ++latestSelection;
      store.getState().beginSelection();

      try {
        await enqueue(async () => {
          if (selection !== latestSelection) return;
          if (
            !current
              .memberships()
              .some((membership) => membership.tenantId === tenantId)
          )
            throw new RangeError(
              `Tenant ${tenantId} is not an active membership.`,
            );

          await current.storage.saveActiveTenantId(tenantId);
          if (selection === latestSelection)
            store.getState().selectTenant(tenantId);
        });
      } catch (error) {
        if (selection === latestSelection) throw error;
      } finally {
        if (selection === latestSelection) store.getState().endSelection();
      }
    },
    clear: async () => {
      ++latestSelection;
      store.getState().clearTenant();
      const current = configuration;
      if (current === undefined) return;
      await enqueue(async () => {
        await Promise.all([
          current.storage.clearActiveTenantId(),
          current.clearMemberships(),
        ]);
      });
    },
    resolve: async (tenantType?: TenantType) => {
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
