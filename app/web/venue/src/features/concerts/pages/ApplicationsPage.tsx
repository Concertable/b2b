import { useParams } from "@tanstack/react-router";
import { useApplicationsByOpportunityQuery } from "@b2b/features/concerts";
import { ApplicationCard } from "../components/ApplicationCard";

export function ApplicationsPage() {
  const { opportunityId } = useParams({ from: "/_venue/my/opportunities/$opportunityId/applications" });
  const { data: applications, isLoading } = useApplicationsByOpportunityQuery(
    Number(opportunityId),
  );

  if (isLoading) return null;

  return (
    <div className="mx-auto max-w-3xl space-y-4 p-6">
      <h1 className="text-xl font-semibold">Applications</h1>
      {applications?.length === 0 && (
        <p className="text-muted-foreground text-sm">No applications yet.</p>
      )}
      {applications?.map((application) => (
        <ApplicationCard key={application.id} application={application} />
      ))}
    </div>
  );
}
