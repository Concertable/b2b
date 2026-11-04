import { z } from "zod";

export const rolePermissionSchema = z.object({
  permission: z.string().min(1),
  audience: z.enum(["AssignedResources", "TenantResources"]),
});

export const roleRequestSchema = z.object({
  name: z.string().trim().min(1, "Enter a role name").max(100),
  isInvitationAssignable: z.boolean(),
  permissions: z.array(rolePermissionSchema),
});

export const updateRoleRequestSchema = roleRequestSchema.extend({
  expectedVersion: z.number().int().nonnegative(),
});

export const retireRoleRequestSchema = z.object({
  expectedVersion: z.number().int().nonnegative(),
  replacementRoleId: z.string().uuid().optional(),
});
