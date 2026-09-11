import { useQuery } from "@tanstack/react-query";
import membersApi from "../api/membersApi";

export const invitationsQueryKey = ["invitations"] as const;

export function useInvitationsQuery() {
  return useQuery({
    queryKey: invitationsQueryKey,
    queryFn: membersApi.listInvitations,
  });
}
