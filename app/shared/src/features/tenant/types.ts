import type { User } from "@concertable/shared/features/auth/types";
import type { TENANT_ROLES } from "./constants";

export type TenantType = "venue" | "artist";
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
  | "ConcertsOpsEdit";

export interface Membership {
  readonly tenantId: string;
  readonly legalName: string;
  readonly type: TenantType;
  readonly role: TenantRole;
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
