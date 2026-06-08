import { Link } from "@tanstack/react-router";
import { Music } from "lucide-react";
import dayjs from "dayjs";
import { useVenueUpcomingConcerts } from "./hooks";
import { DashboardCard, WidgetEmpty, WidgetError, WidgetLoading } from "@/features/dashboard";

export function VenueUpcomingConcertsStrip() {
  const { data, isLoading, isError, refetch } = useVenueUpcomingConcerts();

  return (
    <DashboardCard
      title="Upcoming concerts"
      icon={Music}
      actionLabel="View all"
      actionHref="/_venue/my"
    >
      {isLoading && <WidgetLoading rows={2} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && data.length === 0 && (
        <WidgetEmpty message="No concerts on the books yet." />
      )}
      {data && data.length > 0 && (
        <div className="flex gap-3 overflow-x-auto pb-1">
          {data.map((c) => {
            const pct = c.ticketsTotal === 0
              ? 0
              : Math.round((c.ticketsSold / c.ticketsTotal) * 100);
            return (
              <Link
                key={c.id}
                to={c.href}
                className="bg-card hover:border-primary/40 group flex w-52 shrink-0 flex-col gap-1 rounded-lg border p-3 transition-colors"
              >
                <div className="text-muted-foreground text-xs">
                  {dayjs(c.startDate).format("ddd D MMM")}
                </div>
                <div className="line-clamp-1 text-sm font-medium">{c.name}</div>
                <div className="text-muted-foreground line-clamp-1 text-xs">
                  {c.counterpartyName}
                </div>
                <div className="bg-muted relative mt-1.5 h-1.5 w-full overflow-hidden rounded-full">
                  <div
                    className="absolute inset-y-0 left-0 bg-sky-500"
                    style={{ width: `${pct}%` }}
                  />
                </div>
                <div className="text-muted-foreground flex justify-between text-[10px] tabular-nums">
                  <span>
                    {c.ticketsSold}/{c.ticketsTotal}
                  </span>
                  <span>{pct}%</span>
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </DashboardCard>
  );
}
