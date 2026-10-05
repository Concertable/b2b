import { useMutation, useQueryClient } from "@tanstack/react-query";
import { b2bIdentityKeys } from "@concertable/b2b/features/tenant";
import rolesApi from "../api/rolesApi";
import type { RetireRoleRequest, RoleRequest, UpdateRoleRequest } from "../types";
import { membersQueryKey } from "./useMembersQuery";
import { invitationsQueryKey } from "./useInvitationsQuery";
import { rolesQueryKey } from "./useRolesQuery";

export function useRoleMutations() {
  const queryClient = useQueryClient();
  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: b2bIdentityKeys.all() }),
      queryClient.invalidateQueries({ queryKey: rolesQueryKey() }),
      queryClient.invalidateQueries({ queryKey: membersQueryKey() }),
      queryClient.invalidateQueries({ queryKey: invitationsQueryKey() }),
    ]);
  };
  const create = useMutation({ mutationFn: (request: RoleRequest) => rolesApi.create(request), onSuccess: refresh });
  const update = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateRoleRequest }) => rolesApi.update(id, request),
    onSuccess: refresh,
  });
  const retire = useMutation({
    mutationFn: ({ id, request }: { id: string; request: RetireRoleRequest }) => rolesApi.retire(id, request),
    onSuccess: refresh,
  });
  return { create, update, retire };
}
