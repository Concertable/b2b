import { Star } from "lucide-react";
import {
  DashboardCard,
  RecentReviewsList,
  WidgetError,
  WidgetLoading,
} from "@concertable/web/shared/features/dashboard";
import { useArtistOverviewQuery, useArtistRecentReviewsQuery } from "./hooks";

export function ArtistReviewsWidget() {
  const overview = useArtistOverviewQuery();
  const reviews = useArtistRecentReviewsQuery();

  return (
    <DashboardCard
      title="Reviews"
      icon={Star}
      actionLabel="View all"
      actionHref="/my"
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
