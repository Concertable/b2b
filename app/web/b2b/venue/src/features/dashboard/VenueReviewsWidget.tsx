import { Star } from "lucide-react";
import {
  DashboardCard,
  RecentReviewsList,
  WidgetError,
  WidgetLoading,
} from "@/features/dashboard";
import { useVenueOverviewQuery, useVenueRecentReviewsQuery } from "./hooks";

export function VenueReviewsWidget() {
  const overview = useVenueOverviewQuery();
  const reviews = useVenueRecentReviewsQuery();

  return (
    <DashboardCard
      title="Reviews"
      icon={Star}
      actionLabel="View all"
      actionHref="/_venue/my"
    >
      {reviews.isLoading && <WidgetLoading rows={3} />}
      {reviews.isError && <WidgetError onRetry={() => reviews.refetch()} />}
      {reviews.data && (
        <RecentReviewsList
          summary={overview.data?.reviewSummary}
          items={reviews.data}
        />
      )}
    </DashboardCard>
  );
}
