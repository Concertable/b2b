import type { TenantRole } from "@concertable/b2b/features/tenant/types";

export const TENANT_ROLE_LABELS: Readonly<Record<TenantRole, string>> = {
  owner: "Owner",
  manager: "Manager",
  finance: "Finance",
  staff: "Staff",
  door: "Door",
  sound: "Sound",
};
