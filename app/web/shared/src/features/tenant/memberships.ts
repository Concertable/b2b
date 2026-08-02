import type { Membership, TenantType } from "./types";

export interface TenantResolution {
  readonly memberships: ReadonlyArray<Membership>;
  readonly activeMembership: Membership | undefined;
  readonly selectionRequired: boolean;
}

export function filterMembershipsByPersona(
  memberships: ReadonlyArray<Membership>,
  persona: TenantType,
): ReadonlyArray<Membership> {
  return memberships.filter((membership) => membership.type === persona);
}

export function resolveActiveMembership(
  memberships: ReadonlyArray<Membership>,
  persona: TenantType,
  activeTenantId: string | undefined,
): Membership | undefined {
  const matchingMemberships = filterMembershipsByPersona(memberships, persona);
  return (
    matchingMemberships.find(
      (membership) => membership.tenantId === activeTenantId,
    ) ?? (matchingMemberships.length === 1 ? matchingMemberships[0] : undefined)
  );
}

export function hasPendingTenantChoice(
  memberships: ReadonlyArray<Membership>,
  persona: TenantType,
  activeTenantId: string | undefined,
): boolean {
  const matchingMemberships = filterMembershipsByPersona(memberships, persona);
  return (
    matchingMemberships.length > 1 &&
    !matchingMemberships.some(
      (membership) => membership.tenantId === activeTenantId,
    )
  );
}

export function resolveTenant(
  memberships: ReadonlyArray<Membership>,
  persona: TenantType,
  activeTenantId: string | undefined,
): TenantResolution {
  const matchingMemberships = filterMembershipsByPersona(memberships, persona);
  const activeMembership = resolveActiveMembership(
    memberships,
    persona,
    activeTenantId,
  );

  return {
    memberships: matchingMemberships,
    activeMembership,
    selectionRequired: hasPendingTenantChoice(
      memberships,
      persona,
      activeTenantId,
    ),
  };
}
