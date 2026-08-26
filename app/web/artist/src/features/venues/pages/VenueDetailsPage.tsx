import { DetailsLayout, type DetailsSection } from "@concertable/web/components/details/DetailsLayout";
import { DetailsPageSkeleton } from "@concertable/web/components/skeletons/DetailsPageSkeleton";
import { useVenueById } from "@concertable/shared/features/venues";
import { VenueHero, venueSections } from "@concertable/web/features/venues";
import { VenueOpportunitiesSection } from "../components/VenueOpportunitiesSection";

interface Props {
  id: number;
}

export function VenueDetailsPage({ id }: Readonly<Props>) {
  const { venue, isLoading, isError } = useVenueById(id);

  if (isLoading) return <DetailsPageSkeleton sections={5} />;
  if (isError || !venue)
    return <div className="text-destructive p-6">Venue not found.</div>;

  const hero = <VenueHero venue={venue} />;
  const { about, location, concerts, reviews } = venueSections(venue);

  const opportunities: DetailsSection = {
    id: "opportunities",
    label: "Opportunities",
    content: <VenueOpportunitiesSection venueId={venue.id} />,
  };

  const sections = [about, location, concerts, opportunities, reviews];

  return <DetailsLayout hero={hero} sections={sections} />;
}
