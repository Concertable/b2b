import { useQuery } from "@tanstack/react-query";
import adminApi from "../api/adminApi";

export const adminOverviewQueryKey = ["admin", "overview"] as const;

export function useAdminOverviewQuery() {
  return useQuery({
    queryKey: adminOverviewQueryKey,
    queryFn: adminApi.getOverview,
  });
}
