import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueActivity() {
  return useQuery({
    queryKey: ["dashboard", "venue", "activity"],
    queryFn: dashboardApi.getActivity,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
