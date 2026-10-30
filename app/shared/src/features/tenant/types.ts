import type { User } from "@concertable/shared/features/auth/types";
import type { TENANT_BUSINESS_ACTIVITIES } from "./constants";
import type { TenantPermission } from "./permissions.generated";

export type TenantBusinessActivity = (typeof TENANT_BUSINESS_ACTIVITIES)[number];
export type { TenantPermission } from "./permissions.generated";

export interface RoleSummary {
  readonly id: string;
  readonly name: string;
  readonly isProtectedOwner: boolean;
}

export interface Membership {
  readonly membershipId: string;
  readonly tenantId: string;
  readonly legalName: string;
  readonly roles: ReadonlyArray<RoleSummary>;
  readonly permissionVersion: number;
  readonly rolePolicyVersion: number;
  readonly businessActivities: ReadonlyArray<TenantBusinessActivity>;
  readonly permissions: ReadonlyArray<TenantPermission>;
}

export interface TenantSession {
  readonly generation: number;
  readonly tenantId: string;
  readonly membershipId: string;
  readonly permissionVersion: number;
  readonly rolePolicyVersion: number;
}

export interface TenantSwitchBoundary {
  readonly prepare: (previous: TenantSession | undefined) => Promise<void> | void;
  readonly activate?: (session: TenantSession) => Promise<void> | void;
}

export interface B2bIdentity extends User {
  readonly isAdmin: boolean;
  readonly memberships: ReadonlyArray<Membership>;
}

export interface TenantStorage {
  loadActiveTenantId: () => Promise<string | undefined> | string | undefined;
  saveActiveTenantId: (tenantId: string) => Promise<void> | void;
  clearActiveTenantId: () => Promise<void> | void;
}

export interface TenantSessionConfiguration {
  readonly storage: TenantStorage;
  readonly memberships: () => ReadonlyArray<Membership>;
  readonly clearMemberships: () => Promise<void> | void;
}
