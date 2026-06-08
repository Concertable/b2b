import { Link } from "@tanstack/react-router";
import { CalendarClock, MapPin, Ticket } from "lucide-react";
import dayjs from "dayjs";
import { useArtistUpcomingConcerts } from "./hooks";
import { Skeleton } from "@/components/ui/skeleton";

function daysUntilLabel(iso: string) {
  const diff = dayjs(iso).startOf("day").diff(dayjs().startOf("day"), "day");
  if (diff === 0) return "Today";
  if (diff === 1) return "Tomorrow";
  if (diff < 7) return `In ${diff} days`;
  return dayjs(iso).format("ddd D MMM");
}

export function ArtistNextConcertHero() {
  const { data, isLoading } = useArtistUpcomingConcerts();

  if (isLoading) return <Skeleton className="h-44 w-full rounded-lg" />;
  if (!data || data.length === 0) return null;

  const next = data[0];
  const pct =
    next.ticketsTotal === 0
      ? 0
      : Math.round((next.ticketsSold / next.ticketsTotal) * 100);

  return (
    <Link
      to={next.href}
      className="bg-card hover:border-primary/40 flex flex-col gap-3 rounded-lg border p-5 shadow-sm transition-colors md:flex-row md:items-center"
    >
      <div className="flex flex-col gap-1 md:w-1/3">
        <div className="flex items-center gap-1.5 text-xs font-medium uppercase tracking-wide text-sky-600">
          <CalendarClock className="size-3.5" />
          Next concert
        </div>
        <div className="text-2xl font-semibold">{daysUntilLabel(next.startDate)}</div>
        <div className="text-muted-foreground text-sm">
          {dayjs(next.startDate).format("ddd D MMM, HH:mm")}
        </div>
      </div>

      <div className="flex flex-1 flex-col gap-1">
        <div className="text-lg font-medium">{next.name}</div>
        <div className="text-muted-foreground flex items-center gap-1.5 text-sm">
          <MapPin className="size-3.5" />
          {next.counterpartyName}
        </div>
      </div>

      <div className="flex flex-col gap-1.5 md:w-48">
        <div className="text-muted-foreground flex items-center gap-1.5 text-xs">
          <Ticket className="size-3.5" />
          Tickets sold
        </div>
        <div className="bg-muted relative h-2 w-full overflow-hidden rounded-full">
          <div
            className="absolute inset-y-0 left-0 bg-sky-500"
            style={{ width: `${pct}%` }}
          />
        </div>
        <div className="text-muted-foreground flex justify-between text-xs tabular-nums">
          <span>
            {next.ticketsSold}/{next.ticketsTotal}
          </span>
          <span>{pct}%</span>
        </div>
      </div>
    </Link>
  );
}
