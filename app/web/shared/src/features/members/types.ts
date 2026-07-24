import type { TenantRole } from "@b2b/features/tenant";

export interface Member {
  userId: string;
  email: string;
  role: TenantRole;
}

export interface Invitation {
  id: string;
  email: string;
  role: TenantRole;
  createdAt: string;
  expiresAt: string;
}

export interface ChangeMemberRoleRequest {
  role: TenantRole;
}
