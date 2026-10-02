export const TENANT_HEADER = "X-Tenant-Id";

export const TENANT_ROLES = [
  "owner",
  "manager",
  "finance",
  "staff",
  "door",
  "sound",
] as const;

export const TENANT_BUSINESS_ACTIVITIES = [
  "venueOperator",
  "artist",
  "promoter",
] as const;
