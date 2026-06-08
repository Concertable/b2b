import { Calendar, CircleDollarSign, Inbox, Users } from "lucide-react";
import { useVenueKpis } from "./hooks";
import { formatCurrency } from "@concertable/shared/lib";
import { Skeleton } from "@/components/ui/skeleton";
import { KpiTile } from "@/features/dashboard";

export function VenueKpiStrip() {
  const { data, isLoading } = useVenueKpis();

  if (isLoading || !data) {
    return (
      <>
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="col-span-6 h-24 md:col-span-3" />
        ))}
      </>
    );
  }

  const tiles = [
    {
      label: "Apps to review",
      value: String(data.applicationsToReview),
      delta: data.applicationsToReviewDelta
        ? {
            value: `${data.applicationsToReviewDelta > 0 ? "+" : ""}${data.applicationsToReviewDelta} since yesterday`,
            direction: (data.applicationsToReviewDelta > 0 ? "up" : "down") as "up" | "down",
          }
        : undefined,
      intent: data.applicationsToReview > 0 ? ("urgent" as const) : ("neutral" as const),
      icon: Users,
      href: "/_venue/applications",
    },
    {
      label: "Open opportunities",
      value: String(data.openOpportunities),
      intent: "neutral" as const,
      icon: Inbox,
      href: "/_venue/my",
    },
    {
      label: "Upcoming concerts",
      value: String(data.upcomingConcerts),
      intent: "neutral" as const,
      icon: Calendar,
      href: "/_venue/my",
    },
    {
      label: "MTD revenue",
      value: formatCurrency(data.mtdRevenueCents),
      delta: data.mtdRevenueDeltaPercent
        ? {
            value: `${data.mtdRevenueDeltaPercent > 0 ? "+" : ""}${data.mtdRevenueDeltaPercent}% vs last month`,
            direction: (data.mtdRevenueDeltaPercent > 0 ? "up" : "down") as "up" | "down",
          }
        : undefined,
      intent: "positive" as const,
      icon: CircleDollarSign,
      href: "/_venue/my",
    },
  ];

  return (
    <>
      {tiles.map((t) => (
        <div key={t.label} className="col-span-6 md:col-span-3">
          <KpiTile {...t} />
        </div>
      ))}
    </>
  );
}
