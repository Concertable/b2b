import { Link } from "@tanstack/react-router";
import { Mail } from "lucide-react";
import dayjs from "dayjs";
import { useVenueInboxQuery } from "./hooks";
import { DashboardCard, WidgetEmpty, WidgetError, WidgetLoading } from "@concertable/web/features/dashboard";

export function VenueInboxWidget() {
  const { data, isLoading, isError, refetch } = useVenueInboxQuery();

  return (
    <DashboardCard
      title="Inbox"
      icon={Mail}
      actionLabel="Open inbox"
      actionHref="/_venue/applications"
    >
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && data.length === 0 && (
        <WidgetEmpty message="Inbox is quiet — messages with artists show here." />
      )}
      {data && data.length > 0 && (
        <ul className="divide-border flex flex-col divide-y">
          {data.map((conversation) => (
            <li key={conversation.id} className="py-2.5 first:pt-0 last:pb-0">
              <Link to={conversation.href} className="flex items-start gap-2.5">
                {conversation.unread && (
                  <span className="mt-1.5 size-1.5 shrink-0 rounded-full bg-rose-500" />
                )}
                <div className="min-w-0 flex-1">
                  <div className="flex items-baseline justify-between gap-2">
                    <span
                      className={
                        conversation.unread
                          ? "truncate text-sm font-semibold"
                          : "truncate text-sm"
                      }
                    >
                      {conversation.participants
                        .map((participant) => participant.displayName)
                        .join(", ")}
                    </span>
                    <span className="text-muted-foreground shrink-0 text-xs">
                      {dayjs(conversation.at).format("D MMM")}
                    </span>
                  </div>
                  <p className="text-muted-foreground line-clamp-1 text-xs">
                    {conversation.preview}
                  </p>
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </DashboardCard>
  );
}
