import { useMutation, useQueryClient } from "@tanstack/react-query";
import membersApi from "../api/membersApi";
import { membersQueryKey } from "./useMembersQuery";

export function useRemoveMemberMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: membersApi.removeMember,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: membersQueryKey }),
  });
}
