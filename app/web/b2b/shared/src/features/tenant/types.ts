import type { User } from "@concertable/web/features/auth";
import type { TENANT_ROLES } from "./constants";

export type TenantType = "Venue" | "Artist";
export type TenantRole = (typeof TENANT_ROLES)[number];

export interface Membership {
  readonly tenantId: string;
  readonly legalName: string;
  readonly type: TenantType;
  readonly role: TenantRole;
}

export interface B2bIdentity extends User {
  readonly memberships: ReadonlyArray<Membership>;
}
