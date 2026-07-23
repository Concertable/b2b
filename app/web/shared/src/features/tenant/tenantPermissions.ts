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

// Mirrors the backend SharedPermissions.ByRole (the source of truth); this gate is cosmetic —
// the server re-checks every call via [HasPermission].
const byRole: Record<TenantRole, ReadonlySet<TenantPermission>> = {
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

export function hasPermission(
  role: TenantRole,
  permission: TenantPermission,
): boolean {
  return byRole[role].has(permission);
}
