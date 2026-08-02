import type { Membership, TenantType } from "./types";

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
