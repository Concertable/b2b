import { useMemo } from "react";
import { Users } from "lucide-react";
import dayjs from "dayjs";
import type { ColumnDef } from "@tanstack/react-table";
import {
  useVenueApplicationActions,
  useVenueApplicationsToReviewQuery,
} from "./hooks";
import {
  APPLICATION_ACTION_LABELS,
  type ApplicationActionName,
} from "./applicationActions";
import type { Application } from "./types";
import type { DashboardApplicationStatus } from "@concertable/shared/features/dashboard/types";
import { dealSummary } from "@concertable/web-b2b/features/deals";
import { ConfirmActionDialog } from "@concertable/web-b2b/features/concerts";
import { Button } from "@concertable/web/components/ui/button";
import { DataTable } from "@concertable/web/components/ui/data-table";
import {
  DashboardCard,
  WidgetError,
  WidgetLoading,
} from "@concertable/web/features/dashboard";

const statusPriority: Record<DashboardApplicationStatus, number> = {
  awaitingPayment: 0,
  pending: 1,
  accepted: 2,
  confirmed: 3,
  rejected: 4,
  withdrawn: 5,
};

const statusStyles: Record<
  DashboardApplicationStatus,
  { label: string; chip: string }
> = {
  awaitingPayment: {
    label: "Awaiting payment",
    chip: "bg-amber-50 text-amber-700",
  },
  pending: { label: "Pending", chip: "bg-sky-50 text-sky-700" },
  accepted: { label: "Accepted", chip: "bg-emerald-50 text-emerald-700" },
  confirmed: { label: "Confirmed", chip: "bg-emerald-50 text-emerald-700" },
  rejected: { label: "Rejected", chip: "bg-muted text-muted-foreground" },
  withdrawn: { label: "Withdrawn", chip: "bg-muted text-muted-foreground" },
};

const actionVariants: Record<ApplicationActionName, "default" | "outline"> = {
  accept: "default",
  checkout: "default",
  decline: "outline",
  cancel: "outline",
  contract: "outline",
};

function createColumns(
  onAction: (name: ApplicationActionName, application: Application) => void,
): ColumnDef<Application>[] {
  return [
    {
      accessorKey: "artist",
      header: "Artist",
      cell: ({ row }) => {
        const a = row.original.artist;
        const o = row.original.opportunity;
        return (
          <div className="min-w-0">
            <p className="truncate text-sm font-medium">{a.name}</p>
            <p className="text-muted-foreground text-xs">
              {dayjs(o.startDate).format("ddd D MMM")} · {dealSummary(o.deal)}
            </p>
          </div>
        );
      },
    },
    {
      accessorKey: "status",
      header: "Status",
      cell: ({ row }) => {
        const style = statusStyles[row.original.status];
        return (
          <span
            className={`inline-block rounded-full px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide ${style.chip}`}
          >
            {style.label}
          </span>
        );
      },
    },
    {
      id: "actions",
      header: () => <div className="text-right">Actions</div>,
      cell: ({ row }) => {
        const actionNames = (
          Object.entries(row.original.actions) as [
            ApplicationActionName,
            unknown,
          ][]
        )
          .filter(([, action]) => action !== undefined)
          .map(([name]) => name)
          .filter(
            (name) =>
              name !== "accept" ||
              row.original.actions.checkout === undefined,
          );
        if (actionNames.length === 0) return null;
        return (
          <div className="flex items-center justify-end gap-1">
            {actionNames.map((name) => (
              <Button
                key={name}
                size="xs"
                variant={actionVariants[name]}
                onClick={() => onAction(name, row.original)}
              >
                {APPLICATION_ACTION_LABELS[name]}
              </Button>
            ))}
          </div>
        );
      },
    },
  ];
}

function sortApplications(items: Application[]) {
  return [...items].sort((a, b) => {
    const s = statusPriority[a.status] - statusPriority[b.status];
    if (s !== 0) return s;
    return a.opportunity.startDate.localeCompare(b.opportunity.startDate);
  });
}

export function VenueApplicationsWidget() {
  const { data, isLoading, isError, refetch } =
    useVenueApplicationsToReviewQuery();
  const applicationActions = useVenueApplicationActions();

  const sorted = useMemo(() => (data ? sortApplications(data) : []), [data]);
  const columns = createColumns(applicationActions.request);

  return (
    <DashboardCard
      title="Applications to review"
      icon={Users}
      actionLabel="View all"
      actionHref="/_venue/applications"
    >
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && (
        <DataTable
          columns={columns}
          data={sorted}
          emptyMessage="No applications waiting — share opportunities to attract artists."
        />
      )}
      {applicationActions.confirmation !== undefined && (
        <ConfirmActionDialog
          open
          title={applicationActions.confirmation.title}
          description={applicationActions.confirmation.description}
          dismissLabel="Keep application"
          confirmLabel={applicationActions.confirmation.confirmLabel}
          pendingLabel={applicationActions.confirmation.pendingLabel}
          confirmTestId="dashboard-application-confirm"
          isPending={applicationActions.isPending}
          onDismiss={applicationActions.dismiss}
          onConfirm={applicationActions.confirm}
        />
      )}
    </DashboardCard>
  );
}
