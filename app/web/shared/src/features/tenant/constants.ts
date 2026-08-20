// Mirrors the backend Concertable.B2B.Tenant.Contracts TenantHeaders.TenantId.
export const TENANT_HEADER = "X-Tenant-Id";

// Mirrors the backend TenantRole enum. TenantRole derives from this, so the runtime list and the
// type can't drift.
export const TENANT_ROLES = [
  "owner",
  "manager",
  "finance",
  "staff",
  "door",
  "sound",
] as const;

export const TENANT_ROLE_LABELS: Record<(typeof TENANT_ROLES)[number], string> = {
  owner: "Owner",
  manager: "Manager",
  finance: "Finance",
  staff: "Staff",
  door: "Door",
  sound: "Sound",
};

export function tenantRoleLabel(role: (typeof TENANT_ROLES)[number]): string {
  return TENANT_ROLE_LABELS[role];
}
