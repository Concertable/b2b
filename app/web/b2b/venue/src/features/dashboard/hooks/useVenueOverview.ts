import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueOverview() {
  return useQuery({
    queryKey: ["dashboard", "venue", "overview"],
    queryFn: dashboardApi.getOverview,
    refetchInterval: DASHBOARD_POLLING.static,
  });
}
