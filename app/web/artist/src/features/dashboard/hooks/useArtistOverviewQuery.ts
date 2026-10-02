import { artistDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistOverviewQuery() {
  return useQuery({
    queryKey: artistDashboardKey("overview"),
    queryFn: dashboardApi.getOverview,
    refetchInterval: DASHBOARD_POLLING.static,
  });
}
