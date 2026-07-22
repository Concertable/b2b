import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useArtistInboxQuery() {
  return useQuery({
    queryKey: ["dashboard", "artist", "inbox"],
    queryFn: dashboardApi.getInbox,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
