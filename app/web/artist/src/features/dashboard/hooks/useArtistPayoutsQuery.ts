import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistPayoutsQuery() {
  return useQuery({
    queryKey: ["dashboard", "artist", "payouts"],
    queryFn: dashboardApi.getPayouts,
    refetchInterval: DASHBOARD_POLLING.static,
  });
}
