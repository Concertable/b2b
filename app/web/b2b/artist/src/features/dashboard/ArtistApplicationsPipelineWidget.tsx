import { FileText } from "lucide-react";
import dayjs from "dayjs";
import type { ColumnDef } from "@tanstack/react-table";
import { useArtistApplications } from "./hooks";
import {
  APPLICATION_ACTION_LABELS,
  type ApplicationActionName,
} from "./applicationActions";
import type { Application } from "./types";
import type { DashboardApplicationStatus } from "@concertable/shared/features/dashboard";
import { contractSummary } from "@concertable/shared/features/contracts";
import { Button } from "@/components/ui/button";
import { DataTable } from "@/components/ui/data-table";
import {
  DashboardCard,
  WidgetError,
  WidgetLoading,
} from "@/features/dashboard";

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
  checkout: "default",
  withdraw: "outline",
};

const columns: ColumnDef<Application>[] = [
  {
    accessorKey: "opportunity",
    header: "Venue",
    cell: ({ row }) => {
      const o = row.original.opportunity;
      return (
        <div className="min-w-0">
          <p className="truncate text-sm font-medium">{o.venueName}</p>
          <p className="text-muted-foreground text-xs">
            {dayjs(o.startDate).format("ddd D MMM")} · {contractSummary(o.contract)}
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
      const actionNames = Object.keys(row.original.actions) as ApplicationActionName[];
      if (actionNames.length === 0) return null;
      return (
        <div className="flex items-center justify-end gap-1">
          {actionNames.map((name) => (
            <Button key={name} size="xs" variant={actionVariants[name]}>
              {APPLICATION_ACTION_LABELS[name]}
            </Button>
          ))}
        </div>
      );
    },
  },
];

export function ArtistApplicationsPipelineWidget() {
  const { data, isLoading, isError, refetch } = useArtistApplications();

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
    </DashboardCard>
  );
}
