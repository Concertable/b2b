import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistApplications() {
  return useQuery({
    queryKey: ["dashboard", "artist", "applications"],
    queryFn: dashboardApi.getApplications,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
