import { Calendar, CircleDollarSign, Clock, FileText } from "lucide-react";
import { useArtistKpisQuery } from "./hooks";
import { formatCurrency } from "@concertable/shared/lib";
import { Skeleton } from "@concertable/web/components/ui/skeleton";
import { KpiTile } from "@concertable/web/features/dashboard";

export function ArtistKpiStrip() {
  const { data, isLoading } = useArtistKpisQuery();

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
      label: "Pending applications",
      value: String(data.pendingApplications),
      intent: "neutral" as const,
      icon: FileText,
      href: "/find",
    },
    {
      label: "Awaiting checkout",
      value: String(data.acceptedAwaitingCheckout),
      intent: data.acceptedAwaitingCheckout > 0 ? ("pending" as const) : ("neutral" as const),
      icon: Clock,
      href: "/find",
    },
    {
      label: "Upcoming concerts",
      value: String(data.upcomingConcerts),
      intent: "neutral" as const,
      icon: Calendar,
      href: "/my",
    },
    {
      label: "MTD payouts",
      value: formatCurrency(data.mtdPayoutsCents),
      intent: "positive" as const,
      icon: CircleDollarSign,
      href: "/my",
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
