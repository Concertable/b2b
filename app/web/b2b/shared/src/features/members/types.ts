import type { TenantRole } from "@b2b/features/tenant";

export const INVITE_MEMBER_ROLES = [
  "manager",
  "finance",
  "staff",
  "door",
  "sound",
] as const;

export type InviteMemberRole = (typeof INVITE_MEMBER_ROLES)[number];

export interface InviteMemberRequest {
  email: string;
  role: InviteMemberRole;
}

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
