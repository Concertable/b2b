import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueInbox() {
  return useQuery({
    queryKey: ["dashboard", "venue", "inbox"],
    queryFn: dashboardApi.getInbox,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
