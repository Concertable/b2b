import { useQuery } from "@tanstack/react-query";
import { meQueryKey } from "@/features/user/hooks/useSyncUser";
import {
  filterMembershipsByPersona,
  resolveActiveMembership,
} from "../memberships";
import { permissionsForRole, type TenantPermission } from "../permissions";
import { useActiveTenantId } from "../activeTenant";
import type { B2bIdentity, Membership, TenantType } from "../types";

const EMPTY_MEMBERSHIPS: ReadonlyArray<Membership> = [];

export function useMemberships(persona: TenantType): ReadonlyArray<Membership> {
  const { data } = useQuery<B2bIdentity, Error, ReadonlyArray<Membership>>({
    queryKey: meQueryKey,
    enabled: false,
    select: (identity) =>
      filterMembershipsByPersona(identity.memberships, persona),
  });
  return data ?? EMPTY_MEMBERSHIPS;
}

export function useActiveMembership(
  persona: TenantType,
): Membership | undefined {
  const memberships = useMemberships(persona);
  const activeTenantId = useActiveTenantId();
  return resolveActiveMembership(memberships, persona, activeTenantId);
}

export function usePermissions(
  persona: TenantType,
): ReadonlySet<TenantPermission> {
  const membership = useActiveMembership(persona);
  return permissionsForRole(membership?.role);
}
