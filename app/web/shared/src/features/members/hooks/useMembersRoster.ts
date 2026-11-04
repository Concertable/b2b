import { toast } from "sonner";
import type { MemberRolesRequest } from "../types";
import { useMembersQuery } from "./useMembersQuery";
import { useChangeRoleMutation } from "./useChangeRoleMutation";
import { useRemoveMemberMutation } from "./useRemoveMemberMutation";
import { useRolesQuery } from "./useRolesQuery";

export function useMembersRoster(canManageRoles: boolean) {
  const { data: members, isLoading } = useMembersQuery();
  const { data: roles } = useRolesQuery(canManageRoles);
  const { mutate: mutateRoles, isPending: isUpdating } = useChangeRoleMutation();
  const { mutate: mutateRemove } = useRemoveMemberMutation();
  const changeRoles = (userId: string, request: MemberRolesRequest, onDone: () => void) =>
    mutateRoles({ userId, request }, { onSuccess: () => { toast.success("Roles updated"); onDone(); } });
  const removeMember = (userId: string) =>
    mutateRemove(userId, { onSuccess: () => toast.success("Member removed") });
  return { members, roles: roles ?? [], isLoading, isUpdating, changeRoles, removeMember };
}
