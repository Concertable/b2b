import { Link } from "@tanstack/react-router";
import { Inbox } from "lucide-react";
import dayjs from "dayjs";
import { useVenueOpenOpportunities } from "./hooks";
import { contractSummary } from "@concertable/shared/features/contracts";
import { DashboardCard, WidgetEmpty, WidgetError, WidgetLoading } from "@/features/dashboard";

export function VenueOpenOpportunitiesWidget() {
  const { data, isLoading, isError, refetch } = useVenueOpenOpportunities();

  return (
    <DashboardCard
      title="Open opportunities"
      icon={Inbox}
      actionLabel="View all"
      actionHref="/_venue/my"
    >
      {isLoading && <WidgetLoading rows={3} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && data.length === 0 && (
        <WidgetEmpty message="No opportunities posted — post one to attract artists." />
      )}
      {data && data.length > 0 && (
        <ul className="divide-border flex flex-col divide-y">
          {data.map((o) => (
            <li key={o.opportunity.id} className="py-2.5 first:pt-0 last:pb-0">
              <Link
                to="/my"
                className="flex items-center gap-3 hover:underline"
              >
                <div className="min-w-0 flex-1">
                  <div className="text-sm font-medium">
                    {dayjs(o.opportunity.startDate).format("ddd D MMM")}
                  </div>
                  <div className="text-muted-foreground text-xs">
                    {contractSummary(o.opportunity.contract)}
                  </div>
                </div>
                <div className="text-right">
                  <div className="text-sm tabular-nums">
                    {o.applicationCount}{" "}
                    <span className="text-muted-foreground text-xs">apps</span>
                  </div>
                  <div
                    className={
                      o.daysUntilDeadline <= 3
                        ? "text-xs text-amber-600"
                        : "text-muted-foreground text-xs"
                    }
                  >
                    {o.daysUntilDeadline}d left
                  </div>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </DashboardCard>
  );
}
