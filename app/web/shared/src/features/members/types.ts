import type { RoleSummary, TenantPermission } from "@b2b/features/tenant";
import type { z } from "zod";
import type { inviteMemberRequestSchema } from "./schemas/inviteMemberRequestSchema";
import type { memberRolesRequestSchema } from "./schemas/memberRolesRequestSchema";
import type { roleRequestSchema, updateRoleRequestSchema, retireRoleRequestSchema } from "./schemas/roleRequestSchema";

export type InviteMemberRequest = z.infer<typeof inviteMemberRequestSchema>;
export type MemberRolesRequest = z.infer<typeof memberRolesRequestSchema>;
export type RoleRequest = z.infer<typeof roleRequestSchema>;
export type UpdateRoleRequest = z.infer<typeof updateRoleRequestSchema>;
export type RetireRoleRequest = z.infer<typeof retireRoleRequestSchema>;

export interface Member {
  userId: string;
  email: string;
  roles: ReadonlyArray<RoleSummary>;
}

export interface Invitation {
  id: string;
  email: string;
  roles: ReadonlyArray<RoleSummary>;
  createdAt: string;
  expiresAt: string;
}

export type ResourceAudience = "AssignedResources" | "TenantResources";

export interface RolePermission {
  permission: TenantPermission;
  audience: ResourceAudience;
}

export interface Role {
  id: string;
  name: string;
  version: number;
  isSystemPreset: boolean;
  isProtectedOwner: boolean;
  isInvitationAssignable: boolean;
  permissions: ReadonlyArray<RolePermission>;
}

export interface PermissionMetadata {
  permission: TenantPermission;
  label: string;
  category: string;
  resourceBindings: ReadonlyArray<{ resource: string; facet: string; policy: string; requiresScopes: ReadonlyArray<string> }>;
  assignableAudiences: ReadonlyArray<ResourceAudience>;
  ownerOnly: boolean;
}
