import { OpportunitySection } from "@concertable/b2b/web/shared/features/concerts";
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
