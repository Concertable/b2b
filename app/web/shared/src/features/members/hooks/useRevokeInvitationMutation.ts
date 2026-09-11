import { useMutation, useQueryClient } from "@tanstack/react-query";
import membersApi from "../api/membersApi";
import { invitationsQueryKey } from "./useInvitationsQuery";

export function useRevokeInvitationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: membersApi.revokeInvitation,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: invitationsQueryKey }),
  });
}
