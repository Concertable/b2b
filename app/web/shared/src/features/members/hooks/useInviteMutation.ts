import { useMutation, useQueryClient } from "@tanstack/react-query";
import membersApi from "../api/membersApi";
import { invitationsQueryKey } from "./useInvitationsQuery";

export function useInviteMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: membersApi.invite,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: invitationsQueryKey }),
  });
}
