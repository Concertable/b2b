import { venueDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueActivityQuery() {
  return useQuery({
    queryKey: venueDashboardKey("activity"),
    queryFn: dashboardApi.getActivity,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
