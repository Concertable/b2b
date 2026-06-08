import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueRecentReviews() {
  return useQuery({
    queryKey: ["dashboard", "venue", "recent-reviews"],
    queryFn: dashboardApi.getRecentReviews,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
