export const TENANT_HEADER = "X-Tenant-Id";

export const TENANT_ROLES = [
  "owner",
  "manager",
  "finance",
  "staff",
  "door",
  "sound",
] as const;

export const TENANT_ROLE_LABELS: Record<(typeof TENANT_ROLES)[number], string> =
  {
    owner: "Owner",
    manager: "Manager",
    finance: "Finance",
    staff: "Staff",
    door: "Door",
    sound: "Sound",
  };
