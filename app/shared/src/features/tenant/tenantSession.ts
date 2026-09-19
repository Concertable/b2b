import type { StoreApi } from "zustand/vanilla";
import { resolveTenant } from "./memberships";
import {
  useTenantStore,
  type TenantStoreState,
} from "./store/useTenantStore";
import type {
  Membership,
  TenantSession,
  TenantSessionConfiguration,
  TenantStorage,
  TenantBusinessActivity,
  TenantSwitchBoundary,
} from "./types";

export class TenantSwitchInProgressError extends Error {
  constructor() {
    super("A tenant switch is in progress.");
    this.name = "TenantSwitchInProgressError";
  }
}

export class StaleTenantSessionError extends Error {
  constructor() {
    super("The response belongs to an inactive tenant session.");
    this.name = "StaleTenantSessionError";
  }
}

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
  let generation = 0;
  let pendingSession: TenantSession | undefined;
  let selectionQueue = Promise.resolve();

  const findMembership = (tenantId: string | undefined) =>
    configuration
      ?.memberships()
      .find((membership) => membership.tenantId === tenantId);

  const toSession = (membership: Membership): TenantSession => ({
    generation,
    tenantId: membership.tenantId,
    membershipId: membership.membershipId,
    permissionVersion: membership.permissionVersion,
  });

  const current = (): TenantSession | undefined => {
    const membership = findMembership(store.getState().activeTenantId);
    return membership === undefined ? undefined : toSession(membership);
  };

  const enqueue = <T>(operation: () => Promise<T>) => {
    const queued = selectionQueue.catch(() => undefined).then(operation);
    selectionQueue = queued.then(
      () => undefined,
      () => undefined,
    );
    return queued;
  };

  const sessionApi = {
    configure: async (nextConfiguration: TenantSessionConfiguration) => {
      configuration = nextConfiguration;
      await enqueue(async () => {
        store
          .getState()
          .hydrateTenant(await nextConfiguration.storage.loadActiveTenantId());
      });
    },
    tenantIdForRequest: () => {
      return (pendingSession ?? current())?.tenantId;
    },
    current,
    captureRequest: (isMutation = false) => {
      if (isMutation && store.getState().isSelectionPending)
        throw new TenantSwitchInProgressError();
      return pendingSession ?? current();
    },
    isCurrent: (session: TenantSession) => {
      const active = pendingSession ?? current();
      return (
        active !== undefined &&
        active.generation === session.generation &&
        active.tenantId === session.tenantId &&
        active.membershipId === session.membershipId &&
        active.permissionVersion === session.permissionVersion
      );
    },
    beginSwitch: () => {
      const previous = current();
      const selection = ++latestSelection;
      generation += 1;
      pendingSession = undefined;
      store.getState().beginSelection();
      return { generation, previous, selection };
    },
    select: async (
      tenantId: string,
      token?: {
        readonly generation: number;
        readonly previous: TenantSession | undefined;
        readonly selection: number;
      },
    ) => {
      const current = requireConfiguration(configuration);
      if (
        !current
          .memberships()
          .some((membership) => membership.tenantId === tenantId)
      )
        throw new RangeError(`Tenant ${tenantId} is not an active membership.`);
      const ownsSwitch = token === undefined;
      const activeToken = token ?? (() => {
        const previous = findMembership(store.getState().activeTenantId);
        const selection = ++latestSelection;
        generation += 1;
        store.getState().beginSelection();
        return {
          generation,
          previous: previous === undefined ? undefined : toSession(previous),
          selection,
        };
      })();

      let selectedSession: TenantSession | undefined;
      try {
        await enqueue(async () => {
          if (activeToken.selection !== latestSelection) return;
          if (
            !current
              .memberships()
              .some((membership) => membership.tenantId === tenantId)
          )
            throw new RangeError(
              `Tenant ${tenantId} is not an active membership.`,
            );

          await current.storage.saveActiveTenantId(tenantId);
          if (activeToken.selection === latestSelection) {
            store.getState().selectTenant(tenantId);
            const membership = findMembership(tenantId);
            if (membership === undefined)
              throw new RangeError(`Tenant ${tenantId} is not an active membership.`);
            selectedSession = toSession(membership);
            pendingSession = selectedSession;
          }
        });
      } catch (error) {
        if (activeToken.selection === latestSelection) throw error;
      } finally {
        if (ownsSwitch && activeToken.selection === latestSelection) {
          pendingSession = undefined;
          store.getState().endSelection();
        }
      }
      if (selectedSession === undefined) {
        if (ownsSwitch) return undefined;
        throw new StaleTenantSessionError();
      }
      if (ownsSwitch) return undefined;
      return selectedSession;
    },
    completeSwitch: (session: TenantSession) => {
      if (session.generation !== generation) return;
      pendingSession = undefined;
      store.getState().endSelection();
    },
    switchTo: async (tenantId: string, boundary: TenantSwitchBoundary) => {
      const currentConfiguration = requireConfiguration(configuration);
      const token = sessionApi.beginSwitch();
      try {
        await boundary.prepare(token.previous);
        const selected = await sessionApi.select(tenantId, token);
        if (selected === undefined) throw new StaleTenantSessionError();
        await boundary.activate?.(selected);
        if (token.selection === latestSelection)
          sessionApi.completeSwitch(selected);
        return selected;
      } catch (error) {
        if (token.selection === latestSelection) {
          pendingSession = undefined;
          const previousMembership = findMembership(token.previous?.tenantId);
          if (previousMembership === undefined) {
            store.getState().clearTenant();
            await currentConfiguration.storage.clearActiveTenantId();
          } else {
            store.getState().selectTenant(previousMembership.tenantId);
            store.getState().endSelection();
            await currentConfiguration.storage.saveActiveTenantId(
              previousMembership.tenantId,
            );
          }
        }
        throw error;
      }
    },
    clear: async () => {
      ++latestSelection;
      generation += 1;
      pendingSession = undefined;
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
    resolve: async (businessActivity?: TenantBusinessActivity) => {
      const current = requireConfiguration(configuration);
      const memberships = current.memberships();
      const selection = latestSelection;
      const activeTenantId = await enqueue(async () => {
        if (selection !== latestSelection)
          return store.getState().activeTenantId;
        const previousTenantId = store.getState().activeTenantId;
        const nextTenantId = store
          .getState()
          .synchronizeTenant(memberships, businessActivity);
        if (nextTenantId !== previousTenantId) {
          generation += 1;
          await persistSelection(current.storage, nextTenantId);
        }
        return nextTenantId;
      });
      return resolveTenant(memberships, businessActivity, activeTenantId);
    },
  };

  return sessionApi;
}

export const tenantSession = createTenantSession(useTenantStore);
