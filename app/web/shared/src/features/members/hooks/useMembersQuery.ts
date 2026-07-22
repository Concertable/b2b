import { useQuery } from "@tanstack/react-query";
import membersApi from "../api/membersApi";

export const membersQueryKey = ["members"] as const;

export function useMembersQuery() {
  return useQuery({
    queryKey: membersQueryKey,
    queryFn: membersApi.listMembers,
  });
}
