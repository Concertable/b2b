import { artistDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { conversationApi } from "@concertable/web-b2b/features/conversations";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";

export function useArtistInboxQuery() {
  return useQuery({
    queryKey: artistDashboardKey("inbox"),
    queryFn: conversationApi.getPreviews,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
