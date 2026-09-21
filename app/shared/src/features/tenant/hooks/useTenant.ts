import { useEffect, useMemo } from "react";
import { resolveTenant } from "../memberships";
import { useTenantStore } from "../store/useTenantStore";
import { tenantSession } from "../tenantSession";
import type { Membership, TenantBusinessActivity } from "../types";

export function useTenant(
  memberships: ReadonlyArray<Membership>,
  businessActivity?: TenantBusinessActivity,
) {
  const activeTenantId = useTenantStore((state) => state.activeTenantId);
  const isSelectionPending = useTenantStore(
    (state) => state.isSelectionPending,
  );
  const resolution = resolveTenant(memberships, businessActivity, activeTenantId);

  useEffect(() => {
    void tenantSession.resolve(businessActivity);
  }, [memberships, businessActivity]);

  const permissions = useMemo(
    () => new Set(resolution.activeMembership?.permissions ?? []),
    [resolution.activeMembership],
  );
  const session = useMemo(
    () => (isSelectionPending ? undefined : tenantSession.current()),
    [
      isSelectionPending,
      resolution.activeMembership?.membershipId,
      resolution.activeMembership?.permissionVersion,
      resolution.activeMembership?.tenantId,
    ],
  );

  return {
    ...resolution,
    permissions,
    session,
    isSelectionPending,
  };
}
