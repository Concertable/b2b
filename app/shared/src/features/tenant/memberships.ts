import type { Membership, TenantBusinessActivity } from "./types";

export interface TenantResolution {
  readonly memberships: ReadonlyArray<Membership>;
  readonly activeMembership: Membership | undefined;
  readonly selectionRequired: boolean;
}

export function filterMembershipsByActivity(
  memberships: ReadonlyArray<Membership>,
  businessActivity?: TenantBusinessActivity,
): ReadonlyArray<Membership> {
  return businessActivity === undefined
    ? memberships
    : memberships.filter((membership) =>
        membership.businessActivities.includes(businessActivity),
      );
}

export function resolveActiveMembership(
  memberships: ReadonlyArray<Membership>,
  businessActivity: TenantBusinessActivity | undefined,
  activeTenantId: string | undefined,
): Membership | undefined {
  const matchingMemberships = filterMembershipsByActivity(
    memberships,
    businessActivity,
  );
  return (
    matchingMemberships.find(
      (membership) => membership.tenantId === activeTenantId,
    ) ?? (matchingMemberships.length === 1 ? matchingMemberships[0] : undefined)
  );
}

export function hasPendingTenantChoice(
  memberships: ReadonlyArray<Membership>,
  businessActivity: TenantBusinessActivity | undefined,
  activeTenantId: string | undefined,
): boolean {
  const matchingMemberships = filterMembershipsByActivity(
    memberships,
    businessActivity,
  );
  return (
    matchingMemberships.length > 1 &&
    !matchingMemberships.some(
      (membership) => membership.tenantId === activeTenantId,
    )
  );
}

export function resolveTenant(
  memberships: ReadonlyArray<Membership>,
  businessActivity: TenantBusinessActivity | undefined,
  activeTenantId: string | undefined,
): TenantResolution {
  return {
    memberships: filterMembershipsByActivity(memberships, businessActivity),
    activeMembership: resolveActiveMembership(
      memberships,
      businessActivity,
      activeTenantId,
    ),
    selectionRequired: hasPendingTenantChoice(
      memberships,
      businessActivity,
      activeTenantId,
    ),
  };
}
