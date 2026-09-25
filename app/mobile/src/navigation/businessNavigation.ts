import type { TenantPermission } from "@concertable/b2b/features/tenant/types";

export function includesOperationsTab(
  permissions: ReadonlySet<TenantPermission>,
) {
  return permissions.has("operations.view");
}
