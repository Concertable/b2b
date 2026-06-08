import { Activity } from "lucide-react";
import { useArtistActivity } from "./hooks";
import { ActivityFeed, DashboardCard, WidgetError, WidgetLoading } from "@/features/dashboard";

export function ArtistActivityWidget() {
  const { data, isLoading, isError, refetch } = useArtistActivity();

  return (
    <DashboardCard title="Activity" icon={Activity}>
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && <ActivityFeed items={data} />}
    </DashboardCard>
  );
}
