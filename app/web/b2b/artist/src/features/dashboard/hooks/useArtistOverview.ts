import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistOverview() {
  return useQuery({
    queryKey: ["dashboard", "artist", "overview"],
    queryFn: dashboardApi.getOverview,
    refetchInterval: DASHBOARD_POLLING.static,
  });
}
