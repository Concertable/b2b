import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistUpcomingConcerts() {
  return useQuery({
    queryKey: ["dashboard", "artist", "upcoming-concerts"],
    queryFn: dashboardApi.getUpcomingConcerts,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
