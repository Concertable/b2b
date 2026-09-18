import { useEffect } from "react";
import { resolveTenant } from "../memberships";
import { useTenantStore } from "../store/useTenantStore";
import { tenantSession } from "../tenantSession";
import type { Membership, TenantBusinessProfile } from "../types";

export function useTenant(
  memberships: ReadonlyArray<Membership>,
  businessProfile?: TenantBusinessProfile,
) {
  const activeTenantId = useTenantStore((state) => state.activeTenantId);
  const isSelectionPending = useTenantStore(
    (state) => state.isSelectionPending,
  );
  const resolution = resolveTenant(memberships, businessProfile, activeTenantId);

  useEffect(() => {
    if (memberships.length > 0) void tenantSession.resolve(businessProfile);
  }, [memberships, businessProfile]);

  return {
    ...resolution,
    permissions: new Set(resolution.activeMembership?.permissions ?? []),
    isSelectionPending,
    selectTenant: tenantSession.select,
  };
}
