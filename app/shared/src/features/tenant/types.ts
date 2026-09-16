import type { User } from "@concertable/shared/features/auth/types";
import type { TENANT_BUSINESS_PROFILES, TENANT_ROLES } from "./constants";

/** A kind of marketplace work a business has activated. A business holds zero or more. */
export type TenantBusinessProfile = (typeof TENANT_BUSINESS_PROFILES)[number];
export type TenantRole = (typeof TENANT_ROLES)[number];

export type TenantPermission =
  | "OperationsView"
  | "ProfileEdit"
  | "PayoutsManage"
  | "SettlementView"
  | "SettlementTrigger"
  | "TenantSettingsEdit"
  | "TenantDelete"
  | "MembersInvite"
  | "MembersRemove"
  | "MembersManageRoles"
  | "MessagesRead"
  | "MessagesSend"
  | "ConcertsOpsEdit"
  | "ConcertsCheckIn"
  | "OpportunitiesManage"
  | "ApplicationsDecide"
  | "ApplicationsSubmit"
  | "ConcertsManage"
  | "ResourcesShare";

export interface Membership {
  readonly tenantId: string;
  readonly legalName: string;
  readonly role: TenantRole;
  readonly businessProfiles: ReadonlyArray<TenantBusinessProfile>;
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
