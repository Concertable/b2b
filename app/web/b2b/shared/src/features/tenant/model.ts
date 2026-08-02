import { requireBusinessAuth, redirectToBusiness } from "@/features/auth";
import { queryClient } from "@/lib/queryClient";
import { meQueryKey } from "@/features/user/hooks/useSyncUser";
import type { B2bIdentity, Membership, TenantType } from "./types";
import { useActiveTenantStore } from "./store/useActiveTenantStore";
import {
  filterMembershipsByPersona,
  hasPendingTenantChoice,
} from "./memberships";

function cachedMemberships(): ReadonlyArray<Membership> {
  return queryClient.getQueryData<B2bIdentity>(meQueryKey)?.memberships ?? [];
}

export async function requireBusinessPersona(
  persona: TenantType,
): Promise<void> {
  await requireBusinessAuth();
  if (filterMembershipsByPersona(cachedMemberships(), persona).length === 0)
    return redirectToBusiness();
}

export function getTenantChoicePending(persona: TenantType): boolean {
  return hasPendingTenantChoice(
    cachedMemberships(),
    persona,
    useActiveTenantStore.getState().activeTenantId,
  );
}

export function reconcileActiveTenant(persona: TenantType): void {
  const { activeTenantId, setActiveTenant } = useActiveTenantStore.getState();
  if (!activeTenantId) return;
  if (
    !filterMembershipsByPersona(cachedMemberships(), persona).some(
      (membership) => membership.tenantId === activeTenantId,
    )
  )
    setActiveTenant(null);
}
