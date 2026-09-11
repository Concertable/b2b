import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueApplicationsToReviewQuery() {
  return useQuery({
    queryKey: ["dashboard", "venue", "applications-to-review"],
    queryFn: dashboardApi.getApplicationsToReview,
    refetchInterval: DASHBOARD_POLLING.fast,
  });
}
