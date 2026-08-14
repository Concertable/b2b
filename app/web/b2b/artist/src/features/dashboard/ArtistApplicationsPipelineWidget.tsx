import { useState } from "react";
import { FileText } from "lucide-react";
import dayjs from "dayjs";
import type { ColumnDef } from "@tanstack/react-table";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useArtistApplicationsQuery } from "./hooks";
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
  withdraw: "outline",
  contract: "default",
};

function createColumns(
  onAction: (name: ApplicationActionName, application: Application) => void,
): ColumnDef<Application>[] {
  return [
    {
      accessorKey: "opportunity",
      header: "Venue",
      cell: ({ row }) => {
        const o = row.original.opportunity;
        return (
          <div className="min-w-0">
            <p className="truncate text-sm font-medium">{o.venueName}</p>
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
          .map(([name]) => name);
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

export function ArtistApplicationsPipelineWidget() {
  const { data, isLoading, isError, refetch } = useArtistApplicationsQuery();
  const queryClient = useQueryClient();
  const [withdrawal, setWithdrawal] = useState<Application | null>(null);
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
      if (name === "withdraw") {
        toast.success("Application withdrawn.");
        void queryClient.invalidateQueries({
          queryKey: ["dashboard", "artist"],
        });
        void queryClient.invalidateQueries({ queryKey: ["applications"] });
      }
      setWithdrawal(null);
    },
  });

  function handleAction(name: ApplicationActionName, application: Application) {
    if (name === "withdraw") {
      setWithdrawal(application);
      return;
    }
    mutation.mutate({ name, application });
  }

  const columns = createColumns(handleAction);

  return (
    <DashboardCard
      title="My applications"
      icon={FileText}
      actionLabel="View all"
      actionHref="/find"
    >
      {isLoading && <WidgetLoading rows={4} />}
      {isError && <WidgetError onRetry={() => refetch()} />}
      {data && (
        <DataTable
          columns={columns}
          data={data}
          emptyMessage="No applications yet — find opportunities to apply to."
        />
      )}
      <ConfirmActionDialog
        open={withdrawal != null}
        title="Withdraw this application?"
        description="Your application will be withdrawn and any payment made towards it will be refunded in full."
        dismissLabel="Keep application"
        confirmLabel="Withdraw application"
        pendingLabel="Withdrawing..."
        confirmTestId="dashboard-withdraw-confirm"
        isPending={mutation.isPending}
        onDismiss={() => setWithdrawal(null)}
        onConfirm={() => {
          if (withdrawal)
            mutation.mutate({ name: "withdraw", application: withdrawal });
        }}
      />
    </DashboardCard>
  );
}
