import { useCallback } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import { requireBusinessAuth, redirectToBusiness } from "@/features/auth";
import { queryClient } from "@/lib/queryClient";
import { meQueryKey } from "@/features/user/hooks/useSyncUser";
import identityApi from "./api/identityApi";
import type { B2bIdentity, Membership, TenantType } from "./types";
import { useActiveTenantStore } from "./store/useActiveTenantStore";
import { hasPermission, type TenantPermission } from "./tenantPermissions";

function forPersona(
  memberships: Membership[],
  persona: TenantType,
): Membership[] {
  return memberships.filter((m) => m.type === persona);
}

function resolveActiveMembership(
  memberships: Membership[],
  persona: TenantType,
  activeTenantId: string | null,
): Membership | undefined {
  const same = forPersona(memberships, persona);
  return (
    same.find((m) => m.tenantId === activeTenantId) ??
    (same.length === 1 ? same[0] : undefined)
  );
}

function choicePending(
  memberships: Membership[],
  persona: TenantType,
  activeTenantId: string | null,
): boolean {
  const same = forPersona(memberships, persona);
  return same.length > 1 && !same.some((m) => m.tenantId === activeTenantId);
}

function cachedMemberships(): Membership[] {
  return queryClient.getQueryData<B2bIdentity>(meQueryKey)?.memberships ?? [];
}

export async function requireBusinessPersona(persona: TenantType): Promise<void> {
  await requireBusinessAuth();
  if (forPersona(cachedMemberships(), persona).length === 0)
    return redirectToBusiness();
}

export function getTenantChoicePending(persona: TenantType): boolean {
  return choicePending(
    cachedMemberships(),
    persona,
    useActiveTenantStore.getState().activeTenantId,
  );
}

// A persisted selection can outlive the membership it names (removed from the tenant, or a
// switched persona). Left in place it replays as a stale X-Tenant-Id and fails every call closed.
export function reconcileActiveTenant(persona: TenantType): void {
  const { activeTenantId, setActiveTenant } = useActiveTenantStore.getState();
  if (!activeTenantId) return;
  if (
    !forPersona(cachedMemberships(), persona).some(
      (m) => m.tenantId === activeTenantId,
    )
  )
    setActiveTenant(null);
}

// Reader only — useSyncUser (root) owns fetching /me; this subscribes to the same cache entry.
export function useMemberships(): Membership[] {
  const { data } = useQuery({
    queryKey: meQueryKey,
    queryFn: identityApi.getMe,
    enabled: false,
  });
  return data?.memberships ?? [];
}

export function useSamePersonaMemberships(persona: TenantType): Membership[] {
  return forPersona(useMemberships(), persona);
}

export function useActiveMembership(persona: TenantType): Membership | undefined {
  const memberships = useMemberships();
  const activeTenantId = useActiveTenantStore((s) => s.activeTenantId);
  return resolveActiveMembership(memberships, persona, activeTenantId);
}

export function useTenantChoicePending(persona: TenantType): boolean {
  const memberships = useMemberships();
  const activeTenantId = useActiveTenantStore((s) => s.activeTenantId);
  return choicePending(memberships, persona, activeTenantId);
}

export function useSelectTenant(): (tenantId: string) => void {
  const router = useRouter();
  const qc = useQueryClient();
  const setActiveTenant = useActiveTenantStore((s) => s.setActiveTenant);
  return useCallback(
    (tenantId: string) => {
      setActiveTenant(tenantId);
      void router.invalidate();
      void qc.invalidateQueries();
    },
    [router, qc, setActiveTenant],
  );
}

export function useHasPermission(
  persona: TenantType,
  permission: TenantPermission,
): boolean {
  const active = useActiveMembership(persona);
  return active ? hasPermission(active.role, permission) : false;
}
