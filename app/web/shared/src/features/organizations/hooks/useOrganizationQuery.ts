import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import organizationApi from "../api/organizationApi";

export const organizationQueryKey = () =>
  currentPrivateQueryKey("organization");

export function useOrganizationQuery() {
  return useQuery({
    queryKey: organizationQueryKey(),
    queryFn: organizationApi.get,
  });
}
