import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import membersApi from "../api/membersApi";

export const membersQueryKey = () => currentPrivateQueryKey("members");

export function useMembersQuery() {
  return useQuery({
    queryKey: membersQueryKey(),
    queryFn: membersApi.listMembers,
  });
}
