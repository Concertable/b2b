import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistPayouts() {
  return useQuery({
    queryKey: ["dashboard", "artist", "payouts"],
    queryFn: dashboardApi.getPayouts,
    refetchInterval: DASHBOARD_POLLING.static,
  });
}
