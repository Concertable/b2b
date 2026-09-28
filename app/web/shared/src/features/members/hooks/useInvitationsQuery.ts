import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import membersApi from "../api/membersApi";

export const invitationsQueryKey = () =>
  currentPrivateQueryKey("invitations");

export function useInvitationsQuery() {
  return useQuery({
    queryKey: invitationsQueryKey(),
    queryFn: membersApi.listInvitations,
  });
}
