export { TENANT_HEADER } from "@concertable/b2b/features/tenant";
export type {
  B2bIdentity,
  Membership,
  TenantPermission,
  RoleSummary,
  TenantBusinessActivity,
} from "@concertable/b2b/features/tenant/types";
export { useTenant, useTenantIdentity } from "./hooks/useTenant";
export { resolveTenantRoute, requireLocalB2bAuth } from "./guards";
export { TenantSwitcher } from "./components/TenantSwitcher";
export { TenantChooser } from "./components/TenantChooser";
