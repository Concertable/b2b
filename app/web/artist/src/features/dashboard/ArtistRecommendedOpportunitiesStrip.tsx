import { Link } from "@tanstack/react-router";
import { Sparkles } from "lucide-react";
import dayjs from "dayjs";
import { useArtistRecommendedOpportunities } from "./hooks";
import { contractSummary } from "@concertable/shared/features/contracts";
import { genreLabel } from "@concertable/shared/types";
import { DashboardCard, WidgetEmpty, WidgetError, WidgetLoading } from "@/features/dashboard";

export function ArtistRecommendedOpportunitiesStrip() {
  const { data, isLoading, isError, refetch } = useArtistRecommendedOpportunities();

  return (
    <DashboardCard
      title="Recommended for you"
      icon={Sparkles}
      actionLabel="Explore all"
      actionHref="/find"
    >
      {isLoading && <WidgetLoading rows={2} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && data.length === 0 && (
        <WidgetEmpty message="No recommendations yet — set your genres to get matches." />
      )}
      {data && data.length > 0 && (
        <div className="flex gap-3 overflow-x-auto pb-1">
          {data.map((o) => (
            <Link
              key={o.id}
              to={o.href}
              className="bg-card hover:border-primary/40 group flex w-56 shrink-0 flex-col gap-1 rounded-lg border p-3 transition-colors"
            >
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground text-xs">
                  {dayjs(o.startDate).format("ddd D MMM")}
                </span>
                {o.fitScore !== undefined && (
                  <span className="rounded-full bg-emerald-50 px-1.5 py-0.5 text-[10px] font-medium text-emerald-700">
                    {o.fitScore}% match
                  </span>
                )}
              </div>
              <div className="line-clamp-1 text-sm font-medium">{o.venueName}</div>
              <div className="text-muted-foreground line-clamp-1 text-xs">
                {o.town}, {o.county}
              </div>
              <div className="text-muted-foreground line-clamp-1 text-xs">
                {contractSummary(o.contract)}
              </div>
              <div className="text-muted-foreground line-clamp-1 text-[11px]">
                {o.genres.map((g) => genreLabel(g)).join(" · ")}
              </div>
            </Link>
          ))}
        </div>
      )}
    </DashboardCard>
  );
}
