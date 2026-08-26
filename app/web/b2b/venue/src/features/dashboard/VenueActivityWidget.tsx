import { Activity } from "lucide-react";
import { useVenueActivityQuery } from "./hooks";
import { ActivityFeed, DashboardCard, WidgetError, WidgetLoading } from "@concertable/web/features/dashboard";

export function VenueActivityWidget() {
  const { data, isLoading, isError, refetch } = useVenueActivityQuery();

  return (
    <DashboardCard title="Activity" icon={Activity}>
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && <ActivityFeed items={data} />}
    </DashboardCard>
  );
}
