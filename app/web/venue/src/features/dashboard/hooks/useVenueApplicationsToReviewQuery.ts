import { venueDashboardKey } from "../queryKeys";
import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueApplicationsToReviewQuery() {
  return useQuery({
    queryKey: venueDashboardKey("applications-to-review"),
    queryFn: dashboardApi.getApplicationsToReview,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
