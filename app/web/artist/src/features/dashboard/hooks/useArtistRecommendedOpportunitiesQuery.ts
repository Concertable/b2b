import { artistDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistRecommendedOpportunitiesQuery() {
  return useQuery({
    queryKey: artistDashboardKey("recommended-opportunities"),
    queryFn: dashboardApi.getRecommendedOpportunities,
    refetchInterval: DASHBOARD_POLLING.static,
  });
}
