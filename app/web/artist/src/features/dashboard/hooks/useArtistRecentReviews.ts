import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistRecentReviews() {
  return useQuery({
    queryKey: ["dashboard", "artist", "recent-reviews"],
    queryFn: dashboardApi.getRecentReviews,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
