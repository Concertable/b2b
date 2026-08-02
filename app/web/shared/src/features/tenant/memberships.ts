import type { Membership, TenantType } from "./types";

export interface TenantResolution {
  readonly memberships: ReadonlyArray<Membership>;
  readonly activeMembership: Membership | undefined;
  readonly selectionRequired: boolean;
}

export function filterMembershipsByTenantType(
  memberships: ReadonlyArray<Membership>,
  tenantType: TenantType,
): ReadonlyArray<Membership> {
  return memberships.filter((membership) => membership.type === tenantType);
}

export function resolveActiveMembership(
  memberships: ReadonlyArray<Membership>,
  tenantType: TenantType,
  activeTenantId: string | undefined,
): Membership | undefined {
  const matchingMemberships = filterMembershipsByTenantType(memberships, tenantType);
  return (
    matchingMemberships.find(
      (membership) => membership.tenantId === activeTenantId,
    ) ?? (matchingMemberships.length === 1 ? matchingMemberships[0] : undefined)
  );
}

export function hasPendingTenantChoice(
  memberships: ReadonlyArray<Membership>,
  tenantType: TenantType,
  activeTenantId: string | undefined,
): boolean {
  const matchingMemberships = filterMembershipsByTenantType(memberships, tenantType);
  return (
    matchingMemberships.length > 1 &&
    !matchingMemberships.some(
      (membership) => membership.tenantId === activeTenantId,
    )
  );
}

export function resolveTenant(
  memberships: ReadonlyArray<Membership>,
  tenantType: TenantType,
  activeTenantId: string | undefined,
): TenantResolution {
  const matchingMemberships = filterMembershipsByTenantType(memberships, tenantType);
  const activeMembership = resolveActiveMembership(
    memberships,
    tenantType,
    activeTenantId,
  );

  return {
    memberships: matchingMemberships,
    activeMembership,
    selectionRequired: hasPendingTenantChoice(
      memberships,
      tenantType,
      activeTenantId,
    ),
  };
}
