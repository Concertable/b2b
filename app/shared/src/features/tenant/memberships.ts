import type { Membership, TenantBusinessProfile } from "./types";

export interface TenantResolution {
  readonly memberships: ReadonlyArray<Membership>;
  readonly activeMembership: Membership | undefined;
  readonly selectionRequired: boolean;
}

/**
 * The memberships eligible for a surface. A profile surface admits any business that has activated that
 * profile; the neutral business surface passes no profile and admits every membership, including a
 * business that has activated none.
 */
export function filterMembershipsByProfile(
  memberships: ReadonlyArray<Membership>,
  businessProfile?: TenantBusinessProfile,
): ReadonlyArray<Membership> {
  return businessProfile === undefined
    ? memberships
    : memberships.filter((membership) =>
        membership.businessProfiles.includes(businessProfile),
      );
}

export function resolveActiveMembership(
  memberships: ReadonlyArray<Membership>,
  businessProfile: TenantBusinessProfile | undefined,
  activeTenantId: string | undefined,
): Membership | undefined {
  const matchingMemberships = filterMembershipsByProfile(
    memberships,
    businessProfile,
  );
  return (
    matchingMemberships.find(
      (membership) => membership.tenantId === activeTenantId,
    ) ?? (matchingMemberships.length === 1 ? matchingMemberships[0] : undefined)
  );
}

export function hasPendingTenantChoice(
  memberships: ReadonlyArray<Membership>,
  businessProfile: TenantBusinessProfile | undefined,
  activeTenantId: string | undefined,
): boolean {
  const matchingMemberships = filterMembershipsByProfile(
    memberships,
    businessProfile,
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
  businessProfile: TenantBusinessProfile | undefined,
  activeTenantId: string | undefined,
): TenantResolution {
  return {
    memberships: filterMembershipsByProfile(memberships, businessProfile),
    activeMembership: resolveActiveMembership(
      memberships,
      businessProfile,
      activeTenantId,
    ),
    selectionRequired: hasPendingTenantChoice(
      memberships,
      businessProfile,
      activeTenantId,
    ),
  };
}
