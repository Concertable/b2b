import type { User } from "@/features/auth";
import type { TENANT_ROLES } from "./constants";

export type TenantType = "Venue" | "Artist";
export type TenantRole = (typeof TENANT_ROLES)[number];

export interface Membership {
  tenantId: string;
  legalName: string;
  type: TenantType;
  role: TenantRole;
}

export interface B2bIdentity extends User {
  memberships: Membership[];
}
