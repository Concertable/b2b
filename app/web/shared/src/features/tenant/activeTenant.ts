import { getCachedMemberships } from "./identityCache";
import { filterMembershipsByPersona } from "./memberships";
import { useActiveTenantStore } from "./store/useActiveTenantStore";
import type { TenantType } from "./types";

export function useActiveTenantId(): string | undefined {
  return useActiveTenantStore((state) => state.activeTenantId);
}

export function getActiveTenantId(): string | undefined {
  return useActiveTenantStore.getState().activeTenantId;
}

export function setActiveTenant(tenantId: string): void {
  useActiveTenantStore.getState().setActiveTenant(tenantId);
}

export function clearActiveTenant(): void {
  useActiveTenantStore.getState().setActiveTenant(undefined);
}

export function reconcileActiveTenant(persona: TenantType): void {
  const activeTenantId = getActiveTenantId();
  if (!activeTenantId) return;
  if (
    !filterMembershipsByPersona(getCachedMemberships(), persona).some(
      (membership) => membership.tenantId === activeTenantId,
    )
  )
    clearActiveTenant();
}
