import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistRecommendedOpportunities() {
  return useQuery({
    queryKey: ["dashboard", "artist", "recommended-opportunities"],
    queryFn: dashboardApi.getRecommendedOpportunities,
    refetchInterval: DASHBOARD_POLLING.static,
  });
}
