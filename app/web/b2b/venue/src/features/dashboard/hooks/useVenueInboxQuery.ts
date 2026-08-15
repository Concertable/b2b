import { useQuery } from "@tanstack/react-query";
import { messageApi } from "@concertable/b2b/features/conversations";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";

export function useVenueInboxQuery() {
  return useQuery({
    queryKey: ["dashboard", "venue", "inbox"],
    queryFn: messageApi.getPreviews,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
