import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueKpis() {
  return useQuery({
    queryKey: ["dashboard", "venue", "kpis"],
    queryFn: dashboardApi.getKpis,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
