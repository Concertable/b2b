import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueUpcomingConcerts() {
  return useQuery({
    queryKey: ["dashboard", "venue", "upcoming-concerts"],
    queryFn: dashboardApi.getUpcomingConcerts,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
