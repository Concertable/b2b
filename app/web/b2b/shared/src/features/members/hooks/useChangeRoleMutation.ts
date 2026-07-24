import { useMutation, useQueryClient } from "@tanstack/react-query";
import membersApi from "../api/membersApi";
import type { ChangeMemberRoleRequest } from "../types";
import { membersQueryKey } from "./useMembersQuery";

export function useChangeRoleMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      userId,
      request,
    }: {
      userId: string;
      request: ChangeMemberRoleRequest;
    }) => membersApi.changeRole(userId, request),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: membersQueryKey }),
  });
}
