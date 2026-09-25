import { venueDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueOpenOpportunitiesQuery() {
  return useQuery({
    queryKey: venueDashboardKey("open-opportunities"),
    queryFn: dashboardApi.getOpenOpportunities,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
