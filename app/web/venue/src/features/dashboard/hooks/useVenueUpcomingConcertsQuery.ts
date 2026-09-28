import { venueDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueUpcomingConcertsQuery() {
  return useQuery({
    queryKey: venueDashboardKey("upcoming-concerts"),
    queryFn: dashboardApi.getUpcomingConcerts,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
