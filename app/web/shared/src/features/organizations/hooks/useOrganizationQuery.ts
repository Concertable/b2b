import { useQuery } from "@tanstack/react-query";
import organizationApi from "../api/organizationApi";

export function useOrganizationQuery() {
  return useQuery({
    queryKey: ["organization"],
    queryFn: organizationApi.get,
  });
}
