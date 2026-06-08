import { useNavigate } from "@tanstack/react-router";
import { Button } from "@/components/ui/button";
import { OpportunitySection } from "@b2b/features/concerts";

interface Props {
  venueId: number;
}

export function MyOpportunitiesSection({ venueId }: Readonly<Props>) {
  return (
    <OpportunitySection
      venueId={venueId}
      renderActions={(opportunity) => <ViewApplicationsAction opportunityId={opportunity.id} />}
    />
  );
}

function ViewApplicationsAction({ opportunityId }: Readonly<{ opportunityId: number }>) {
  const navigate = useNavigate();

  return (
    <Button
      size="sm"
      onClick={() =>
        navigate({
          to: "/my/opportunities/$opportunityId/applications",
          params: { opportunityId },
        })
      }
    >
      View Applications
    </Button>
  );
}
