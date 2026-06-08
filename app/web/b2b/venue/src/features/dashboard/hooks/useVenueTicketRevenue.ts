import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenueTicketRevenue() {
  return useQuery({
    queryKey: ["dashboard", "venue", "ticket-revenue"],
    queryFn: dashboardApi.getTicketRevenue,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
