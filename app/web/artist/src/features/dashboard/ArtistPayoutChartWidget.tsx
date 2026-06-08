import { TrendingUp } from "lucide-react";
import { useArtistPayouts } from "./hooks";
import { DashboardCard, MonthlyRevenueChart, WidgetEmpty, WidgetError, WidgetLoading } from "@/features/dashboard";

export function ArtistPayoutChartWidget() {
  const { data, isLoading, isError, refetch } = useArtistPayouts();

  return (
    <DashboardCard title="Payouts" icon={TrendingUp}>
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && data.every((p) => p.grossCents === 0) && (
        <WidgetEmpty message="Once you settle your first gig, payouts land here." />
      )}
      {data && data.some((p) => p.grossCents > 0) && (
        <MonthlyRevenueChart data={data} accent="sky" />
      )}
    </DashboardCard>
  );
}
