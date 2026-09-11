import { toast } from "sonner";
import type { TenantRole } from "@b2b/features/tenant";
import { useMembersQuery } from "./useMembersQuery";
import { useChangeRoleMutation } from "./useChangeRoleMutation";
import { useRemoveMemberMutation } from "./useRemoveMemberMutation";

export function useMembersRoster() {
  const { data: members, isLoading } = useMembersQuery();
  const { mutate: mutateRole } = useChangeRoleMutation();
  const { mutate: mutateRemove } = useRemoveMemberMutation();

  const changeRole = (userId: string, role: TenantRole) =>
    mutateRole(
      { userId, request: { role } },
      { onSuccess: () => toast.success("Role updated") },
    );

  const removeMember = (userId: string) =>
    mutateRemove(userId, { onSuccess: () => toast.success("Member removed") });

  return { members, isLoading, changeRole, removeMember };
}
