import { OpportunitySection } from "@b2b/features/concerts";
import { ApplyAction } from "../../concerts/components/ApplyAction";

interface Props {
  venueId: number;
}

export function VenueOpportunitiesSection({ venueId }: Readonly<Props>) {
  return (
    <OpportunitySection
      venueId={venueId}
      renderActions={(opportunity) => <ApplyAction opportunity={opportunity} />}
    />
  );
}
