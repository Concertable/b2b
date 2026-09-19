import { venueDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueKpisQuery() {
  return useQuery({
    queryKey: venueDashboardKey("kpis"),
    queryFn: dashboardApi.getKpis,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
