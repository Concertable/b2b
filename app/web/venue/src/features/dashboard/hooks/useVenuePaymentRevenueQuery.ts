import { useQuery } from "@tanstack/react-query";
import { DASHBOARD_POLLING } from "@concertable/shared/features/dashboard";
import dashboardApi from "../dashboardApi";

export function useVenuePaymentRevenueQuery() {
  return useQuery({
    queryKey: ["dashboard", "venue", "payment-revenue"],
    queryFn: dashboardApi.getPaymentRevenue,
    refetchInterval: DASHBOARD_POLLING.normal,
  });
}
