import dayjs from "dayjs";
import {
  usePendingApplicationsQuery,
  useRecentDeniedApplicationsQuery,
  ConfirmActionDialog,
} from "@b2b/features/concerts";
import type { Application } from "@b2b/features/concerts";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useWithdrawApplication } from "../hooks/useWithdrawApplication";

interface RowProps {
  application: Application;
  onWithdraw?: (applicationId: number) => void;
}

function ApplicationRow({ application, onWithdraw }: Readonly<RowProps>) {
  const { opportunity, status, actions } = application;

  return (
    <div
      className="border-border bg-card flex items-center justify-between gap-4 rounded-xl border p-4"
      data-testid={`application-${application.id}`}
    >
      <div className="space-y-0.5">
        <p className="font-medium">
          {dayjs(opportunity.startDate).format("D MMM YYYY")} &mdash;{" "}
          {dayjs(opportunity.endDate).format("D MMM YYYY")}
        </p>
        {opportunity.genres.length > 0 && (
          <p className="text-muted-foreground text-sm">
            {opportunity.genres.join(", ")}
          </p>
        )}
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <Badge variant="outline">{status}</Badge>
        {onWithdraw && actions.withdraw && (
          <Button
            size="sm"
            variant="destructive"
            onClick={() => onWithdraw(application.id)}
            data-testid="withdraw"
          >
            Withdraw
          </Button>
        )}
      </div>
    </div>
  );
}

export function MyApplicationsPage() {
  const { data: pending, isLoading: pendingLoading } =
    usePendingApplicationsQuery();
  const { data: denied, isLoading: deniedLoading } =
    useRecentDeniedApplicationsQuery();
  const withdraw = useWithdrawApplication();

  if (pendingLoading || deniedLoading) return null;

  return (
    <div className="mx-auto max-w-3xl space-y-8 p-6">
      <section className="space-y-4">
        <h1 className="text-xl font-semibold">My Applications</h1>
        {pending?.length === 0 && (
          <p className="text-muted-foreground text-sm">
            No open applications.
          </p>
        )}
        {pending?.map((application) => (
          <ApplicationRow
            key={application.id}
            application={application}
            onWithdraw={withdraw.request}
          />
        ))}
      </section>

      <section className="space-y-4">
        <h2 className="text-lg font-semibold">Recently denied</h2>
        {denied?.length === 0 && (
          <p className="text-muted-foreground text-sm">
            No recently denied applications.
          </p>
        )}
        {denied?.map((application) => (
          <ApplicationRow key={application.id} application={application} />
        ))}
      </section>

      <ConfirmActionDialog
        open={withdraw.isOpen}
        title="Withdraw this application?"
        description="Your application is withdrawn and any payment made towards it is refunded in full. This can't be undone."
        dismissLabel="Keep application"
        confirmLabel="Withdraw application"
        pendingLabel="Withdrawing..."
        confirmTestId="withdraw-confirm"
        isPending={withdraw.isPending}
        onDismiss={withdraw.dismiss}
        onConfirm={withdraw.confirm}
      />
    </div>
  );
}
