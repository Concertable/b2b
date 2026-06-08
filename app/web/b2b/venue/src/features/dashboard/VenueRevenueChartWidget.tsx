import { TrendingUp } from "lucide-react";
import { useVenueTicketRevenue } from "./hooks";
import { DashboardCard, MonthlyRevenueChart, WidgetEmpty, WidgetError, WidgetLoading } from "@/features/dashboard";

export function VenueRevenueChartWidget() {
  const { data, isLoading, isError, refetch } = useVenueTicketRevenue();

  return (
    <DashboardCard title="Ticket revenue" icon={TrendingUp}>
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && data.every((p) => p.grossCents === 0) && (
        <WidgetEmpty message="Once tickets sell, the trend lands here." />
      )}
      {data && data.some((p) => p.grossCents > 0) && (
        <MonthlyRevenueChart data={data} accent="emerald" />
      )}
    </DashboardCard>
  );
}
