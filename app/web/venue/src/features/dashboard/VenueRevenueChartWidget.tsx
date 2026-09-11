import { TrendingUp } from "lucide-react";
import { useVenuePaymentRevenueQuery } from "./hooks";
import { DashboardCard, MonthlyRevenueChart, WidgetEmpty, WidgetError, WidgetLoading } from "@concertable/web/features/dashboard";

export function VenueRevenueChartWidget() {
  const { data, isLoading, isError, refetch } = useVenuePaymentRevenueQuery();

  return (
    <DashboardCard title="Revenue" icon={TrendingUp}>
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && data.every((p) => p.grossCents === 0) && (
        <WidgetEmpty message="Once payments land, the trend shows here." />
      )}
      {data && data.some((p) => p.grossCents > 0) && (
        <MonthlyRevenueChart data={data} accent="emerald" />
      )}
    </DashboardCard>
  );
}
