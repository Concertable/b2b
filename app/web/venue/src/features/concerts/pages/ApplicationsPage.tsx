import { useParams } from "@tanstack/react-router";
import {
  useApplicationsByOpportunityQuery,
  ConfirmActionDialog,
} from "@concertable/web-b2b/features/concerts";
import { ApplicationCard } from "../components/ApplicationCard";
import { useDenyApplication } from "../hooks/useDenyApplication";
import { useCancelApplication } from "../hooks/useCancelApplication";

export function ApplicationsPage() {
  const { opportunityId } = useParams({ from: "/_venue/my/opportunities/$opportunityId/applications" });
  const { data: applications, isLoading } = useApplicationsByOpportunityQuery(opportunityId);
  const deny = useDenyApplication(opportunityId);
  const cancel = useCancelApplication(opportunityId);

  if (isLoading) return null;

  return (
    <div className="mx-auto max-w-3xl space-y-4 p-6">
      <h1 className="text-xl font-semibold">Applications</h1>
      {applications?.length === 0 && (
        <p className="text-muted-foreground text-sm">No applications yet.</p>
      )}
      {applications?.map((application) => (
        <ApplicationCard
          key={application.id}
          application={application}
          onDeny={deny.request}
          onCancel={cancel.request}
        />
      ))}

      <ConfirmActionDialog
        open={deny.isOpen}
        title="Deny this application?"
        description="The artist is notified that their application was not selected. This can't be undone."
        dismissLabel="Keep application"
        confirmLabel="Deny application"
        pendingLabel="Denying..."
        confirmTestId="deny-confirm"
        isPending={deny.isPending}
        onDismiss={deny.dismiss}
        onConfirm={deny.confirm}
      />

      <ConfirmActionDialog
        open={cancel.isOpen}
        title="Cancel this application?"
        description="The artist is notified that their application was cancelled. This can't be undone."
        dismissLabel="Keep application"
        confirmLabel="Cancel application"
        pendingLabel="Cancelling..."
        confirmTestId="cancel-application-confirm"
        isPending={cancel.isPending}
        onDismiss={cancel.dismiss}
        onConfirm={cancel.confirm}
      />
    </div>
  );
}
