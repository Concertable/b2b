import { artistDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistActivityQuery() {
  return useQuery({
    queryKey: artistDashboardKey("activity"),
    queryFn: dashboardApi.getActivity,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
