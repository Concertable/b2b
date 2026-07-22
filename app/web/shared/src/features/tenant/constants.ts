// Mirrors the backend Concertable.B2B.Tenant.Contracts TenantHeaders.TenantId.
export const TENANT_HEADER = "X-Tenant-Id";

// Mirrors the backend TenantRole enum. TenantRole derives from this, so the runtime list and the
// type can't drift.
export const TENANT_ROLES = [
  "Owner",
  "Manager",
  "Finance",
  "Staff",
  "Door",
  "Sound",
] as const;
