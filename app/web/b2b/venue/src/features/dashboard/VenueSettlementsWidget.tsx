import { CircleDollarSign } from "lucide-react";
import dayjs from "dayjs";
import { useVenueSettlements } from "./hooks";
import { formatCurrency } from "@concertable/shared/lib";
import { DashboardCard, WidgetEmpty, WidgetError, WidgetLoading } from "@/features/dashboard";

export function VenueSettlementsWidget() {
  const { data, isLoading, isError, refetch } = useVenueSettlements();

  return (
    <DashboardCard title="Recent settlements" icon={CircleDollarSign}>
      {isLoading && <WidgetLoading rows={3} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && data.length === 0 && (
        <WidgetEmpty message="Settlements land here after concerts complete." />
      )}
      {data && data.length > 0 && (
        <ul className="divide-border flex flex-col divide-y">
          {data.map((s) => (
            <li
              key={s.id}
              className="flex items-center justify-between gap-3 py-2 text-sm first:pt-0 last:pb-0"
            >
              <span className="text-muted-foreground w-16 shrink-0 text-xs">
                {dayjs(s.at).format("D MMM")}
              </span>
              <span className="min-w-0 flex-1 truncate">{s.concertName}</span>
              <span className="tabular-nums">
                {formatCurrency(s.amountCents, { fractionDigits: 2 })}
              </span>
              <span className="text-muted-foreground w-32 shrink-0 truncate text-right text-xs">
                {s.direction === "Out" ? "→" : "←"} {s.counterpartyName}
              </span>
            </li>
          ))}
        </ul>
      )}
    </DashboardCard>
  );
}
