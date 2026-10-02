import { artistDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistRecentReviewsQuery() {
  return useQuery({
    queryKey: artistDashboardKey("recent-reviews"),
    queryFn: dashboardApi.getRecentReviews,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
