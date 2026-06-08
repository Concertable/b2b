import { Activity } from "lucide-react";
import { useVenueActivity } from "./hooks";
import { ActivityFeed, DashboardCard, WidgetError, WidgetLoading } from "@/features/dashboard";

export function VenueActivityWidget() {
  const { data, isLoading, isError, refetch } = useVenueActivity();

  return (
    <DashboardCard title="Activity" icon={Activity}>
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && <ActivityFeed items={data} />}
    </DashboardCard>
  );
}
