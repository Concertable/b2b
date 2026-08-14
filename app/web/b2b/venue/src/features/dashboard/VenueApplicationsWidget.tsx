import { useMemo, useState } from "react";
import { Users } from "lucide-react";
import dayjs from "dayjs";
import type { ColumnDef } from "@tanstack/react-table";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { useVenueApplicationsToReviewQuery } from "./hooks";
import {
  APPLICATION_ACTION_LABELS,
  type ApplicationActionName,
} from "./applicationActions";
import type { Application } from "./types";
import type { DashboardApplicationStatus } from "@concertable/shared/features/dashboard";
import { dealSummary } from "@concertable/b2b/features/deals";
import {
  actionLinkApi,
  ConfirmActionDialog,
} from "@concertable/b2b/features/concerts";
import { Button } from "@concertable/web/components/ui/button";
import { DataTable } from "@concertable/web/components/ui/data-table";
import {
  DashboardCard,
  WidgetError,
  WidgetLoading,
} from "@concertable/web/features/dashboard";

const statusPriority: Record<DashboardApplicationStatus, number> = {
  AwaitingPayment: 0,
  Pending: 1,
  Accepted: 2,
  Confirmed: 3,
  Rejected: 4,
  Withdrawn: 5,
};

const statusStyles: Record<
  DashboardApplicationStatus,
  { label: string; chip: string }
> = {
  AwaitingPayment: {
    label: "Awaiting payment",
    chip: "bg-amber-50 text-amber-700",
  },
  Pending: { label: "Pending", chip: "bg-sky-50 text-sky-700" },
  Accepted: { label: "Accepted", chip: "bg-emerald-50 text-emerald-700" },
  Confirmed: { label: "Confirmed", chip: "bg-emerald-50 text-emerald-700" },
  Rejected: { label: "Rejected", chip: "bg-muted text-muted-foreground" },
  Withdrawn: { label: "Withdrawn", chip: "bg-muted text-muted-foreground" },
};

const actionVariants: Record<ApplicationActionName, "default" | "outline"> = {
  accept: "default",
  checkout: "default",
  decline: "outline",
  cancel: "outline",
  contract: "outline",
};

type DestructiveActionName = "decline" | "cancel";

interface PendingAction {
  name: DestructiveActionName;
  application: Application;
}

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
          .filter(([, action]) => action != null)
          .map(([name]) => name)
          .filter(
            (name) =>
              name !== "accept" || row.original.actions.checkout == null,
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
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [pendingAction, setPendingAction] = useState<PendingAction | null>(
    null,
  );
  const mutation = useMutation({
    mutationFn: async ({
      name,
      application,
    }: {
      name: ApplicationActionName;
      application: Application;
    }) => {
      const action = application.actions[name];
      if (action == null) return;
      if (name === "contract") {
        await actionLinkApi.download(action, `contract-${application.id}.pdf`);
        return;
      }
      await actionLinkApi.execute(action);
    },
    onSuccess: (_data, { name }) => {
      if (name !== "contract") {
        toast.success(
          name === "decline"
            ? "Application declined."
            : "Application cancelled.",
        );
        void queryClient.invalidateQueries({
          queryKey: ["dashboard", "venue"],
        });
        void queryClient.invalidateQueries({ queryKey: ["applications"] });
      }
      setPendingAction(null);
    },
  });

  function handleAction(name: ApplicationActionName, application: Application) {
    if (name === "accept" || name === "checkout") {
      void navigate({
        to:
          name === "checkout"
            ? "/applications/$applicationId/checkout"
            : "/applications/$applicationId/accept",
        params: { applicationId: application.id },
      });
      return;
    }
    if (name === "contract") {
      mutation.mutate({ name, application });
      return;
    }
    setPendingAction({ name, application });
  }

  const sorted = useMemo(() => (data ? sortApplications(data) : []), [data]);
  const columns = createColumns(handleAction);

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
      <ConfirmActionDialog
        open={pendingAction != null}
        title={
          pendingAction?.name === "decline"
            ? "Decline this application?"
            : "Cancel this application?"
        }
        description={
          pendingAction?.name === "decline"
            ? "The artist will be notified that their application was declined."
            : "The application will be cancelled and any payment held will be refunded in full."
        }
        dismissLabel="Keep application"
        confirmLabel={
          pendingAction?.name === "decline"
            ? "Decline application"
            : "Cancel application"
        }
        pendingLabel={
          pendingAction?.name === "decline" ? "Declining..." : "Cancelling..."
        }
        confirmTestId="dashboard-application-confirm"
        isPending={mutation.isPending}
        onDismiss={() => setPendingAction(null)}
        onConfirm={() => {
          if (pendingAction) mutation.mutate(pendingAction);
        }}
      />
    </DashboardCard>
  );
}
