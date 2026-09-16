import type { TenantPermission, TenantRole } from "./types";

const EMPTY_PERMISSIONS: ReadonlySet<TenantPermission> = new Set();

/**
 * Mirrors the server catalog, which is keyed on role alone. What a business is allowed to do with a
 * permission is a separate question the server answers per operation, from the business profiles it has
 * activated and the grants it holds on the resource.
 */
const PERMISSIONS_BY_ROLE: Readonly<
  Record<TenantRole, ReadonlySet<TenantPermission>>
> = {
  owner: new Set<TenantPermission>([
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
    "ConcertsCheckIn",
    "OpportunitiesManage",
    "ApplicationsDecide",
    "ApplicationsSubmit",
    "ConcertsManage",
    "ResourcesShare",
  ]),
  manager: new Set<TenantPermission>([
    "OperationsView",
    "ProfileEdit",
    "SettlementView",
    "MembersInvite",
    "MessagesRead",
    "MessagesSend",
    "ConcertsOpsEdit",
    "ConcertsCheckIn",
    "OpportunitiesManage",
    "ApplicationsDecide",
    "ApplicationsSubmit",
    "ConcertsManage",
    "ResourcesShare",
  ]),
  finance: new Set<TenantPermission>([
    "OperationsView",
    "PayoutsManage",
    "SettlementView",
    "SettlementTrigger",
    "MessagesRead",
  ]),
  staff: new Set<TenantPermission>([
    "OperationsView",
    "MessagesRead",
    "MessagesSend",
    "ConcertsOpsEdit",
    "ConcertsCheckIn",
  ]),
  door: new Set<TenantPermission>(["OperationsView", "ConcertsCheckIn"]),
  sound: new Set<TenantPermission>(["OperationsView", "ConcertsOpsEdit"]),
  restrictedParticipant: new Set<TenantPermission>(["OperationsView"]),
};

export function permissionsForRole(
  role: TenantRole | undefined,
): ReadonlySet<TenantPermission> {
  return role === undefined ? EMPTY_PERMISSIONS : PERMISSIONS_BY_ROLE[role];
}
