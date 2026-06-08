import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistKpis() {
  return useQuery({
    queryKey: ["dashboard", "artist", "kpis"],
    queryFn: dashboardApi.getKpis,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
