import type { TenantRole } from "./types";

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

const EMPTY_PERMISSIONS: ReadonlySet<TenantPermission> = new Set();

const PERMISSIONS_BY_ROLE: Readonly<
  Record<TenantRole, ReadonlySet<TenantPermission>>
> = {
  Owner: new Set<TenantPermission>([
    "OperationsView",
    "ProfileEdit",
    "PayoutsManage",
    "SettlementView",
    "SettlementTrigger",
    "TenantSettingsEdit",
    "TenantDelete",
    "MembersInvite",
    "MembersRemove",
    "MembersManageRoles",
    "MessagesRead",
    "MessagesSend",
    "ConcertsOpsEdit",
  ]),
  Manager: new Set<TenantPermission>([
    "OperationsView",
    "ProfileEdit",
    "SettlementView",
    "MembersInvite",
    "MessagesRead",
    "MessagesSend",
    "ConcertsOpsEdit",
  ]),
  Finance: new Set<TenantPermission>([
    "OperationsView",
    "PayoutsManage",
    "SettlementView",
    "SettlementTrigger",
    "MessagesRead",
  ]),
  Staff: new Set<TenantPermission>([
    "OperationsView",
    "MessagesRead",
    "MessagesSend",
    "ConcertsOpsEdit",
  ]),
  Door: new Set<TenantPermission>(["OperationsView"]),
  Sound: new Set<TenantPermission>(["OperationsView", "ConcertsOpsEdit"]),
};

export function permissionsForRole(
  role: TenantRole | undefined,
): ReadonlySet<TenantPermission> {
  return role === undefined ? EMPTY_PERMISSIONS : PERMISSIONS_BY_ROLE[role];
}
