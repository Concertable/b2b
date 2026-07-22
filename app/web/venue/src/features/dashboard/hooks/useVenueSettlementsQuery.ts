import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueSettlementsQuery() {
  return useQuery({
    queryKey: ["dashboard", "venue", "settlements"],
    queryFn: dashboardApi.getSettlements,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
