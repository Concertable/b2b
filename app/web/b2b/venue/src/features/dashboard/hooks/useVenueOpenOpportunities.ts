import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueOpenOpportunities() {
  return useQuery({
    queryKey: ["dashboard", "venue", "open-opportunities"],
    queryFn: dashboardApi.getOpenOpportunities,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
