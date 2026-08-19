import { resolveTenant } from "../memberships";
import { permissionsForRole } from "../permissions";
import { useTenantStore } from "../store/useTenantStore";
import { tenantSession } from "../tenantSession";
import type { Membership, TenantType } from "../types";

export function useTenant(
  memberships: ReadonlyArray<Membership>,
  tenantType: TenantType,
) {
  const activeTenantId = useTenantStore((state) => state.activeTenantId);
  const resolution = resolveTenant(memberships, tenantType, activeTenantId);

  return {
    ...resolution,
    permissions: permissionsForRole(resolution.activeMembership?.role),
    selectTenant: tenantSession.select,
  };
}
