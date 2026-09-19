import { artistDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistApplicationsQuery() {
  return useQuery({
    queryKey: artistDashboardKey("applications"),
    queryFn: dashboardApi.getApplications,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
