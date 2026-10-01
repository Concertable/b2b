import { useMutation, useQueryClient } from "@tanstack/react-query";
import { b2bIdentityKeys } from "@concertable/b2b/features/tenant";
import membersApi from "../api/membersApi";
import type { MemberRolesRequest } from "../types";
import { membersQueryKey } from "./useMembersQuery";
import { invitationsQueryKey } from "./useInvitationsQuery";
import { rolesQueryKey } from "./useRolesQuery";

export function useChangeRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, request }: { userId: string; request: MemberRolesRequest }) =>
      membersApi.changeRoles(userId, request),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: b2bIdentityKeys.all() }),
        queryClient.invalidateQueries({ queryKey: membersQueryKey() }),
        queryClient.invalidateQueries({ queryKey: invitationsQueryKey() }),
        queryClient.invalidateQueries({ queryKey: rolesQueryKey() }),
      ]);
    },
  });
}
