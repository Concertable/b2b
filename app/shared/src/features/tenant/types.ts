import type { User } from "@concertable/shared/features/auth/types";
import type { TENANT_BUSINESS_ACTIVITIES, TENANT_ROLES } from "./constants";

/** A kind of marketplace work a business has activated. A business holds zero or more. */
export type TenantBusinessActivity = (typeof TENANT_BUSINESS_ACTIVITIES)[number];
export type TenantRole = (typeof TENANT_ROLES)[number];

export type TenantPermission =
  | "operations.view"
  | "profile.edit"
  | "payouts.manage"
  | "settlement.view"
  | "settlement.trigger"
  | "tenant.settings.edit"
  | "tenant.delete"
  | "members.invite"
  | "members.remove"
  | "members.manage_roles"
  | "messages.read"
  | "messages.send"
  | "concerts.ops_edit"
  | "concerts.check_in"
  | "opportunities.manage"
  | "applications.decide"
  | "applications.submit"
  | "concerts.manage"
  | "resources.share";

export interface Membership {
  readonly membershipId: string;
  readonly tenantId: string;
  readonly legalName: string;
  readonly role: TenantRole;
  readonly permissionVersion: number;
  readonly businessActivities: ReadonlyArray<TenantBusinessActivity>;
  readonly permissions: ReadonlyArray<TenantPermission>;
}

export interface TenantSession {
  readonly generation: number;
  readonly tenantId: string;
  readonly membershipId: string;
  readonly permissionVersion: number;
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
