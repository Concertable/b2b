import { artistDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistKpisQuery() {
  return useQuery({
    queryKey: artistDashboardKey("kpis"),
    queryFn: dashboardApi.getKpis,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
