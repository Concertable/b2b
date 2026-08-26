import type { User } from "@concertable/shared/features/auth";

export type TenantType = "venue" | "artist";

export interface Membership {
  tenantId: string;
  legalName: string;
  type: TenantType;
}

export interface B2bIdentity extends User {
  memberships: Membership[];
}

export function isB2bIdentity(user: User | null): user is B2bIdentity {
  return user !== null && "memberships" in user && Array.isArray(user.memberships);
}
