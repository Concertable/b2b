import { ConfigBar } from "@/components/ConfigBar";
import { EditableProvider } from "@concertable/shared/providers";
import { DetailsLayout, type DetailsSection } from "@/components/details/DetailsLayout";
import { DetailsPageSkeleton } from "@/components/skeletons/DetailsPageSkeleton";
import { useVenueStore, VenueHero, venueSections } from "@/features/venues";
import { useMyVenue } from "../hooks/useMyVenue";
import { MyOpportunitiesSection } from "../components/MyOpportunitiesSection";

export function MyVenuePage() {
  const { venue, isDirty, isSaving, isLoading, save, resetDraft, toggleEdit, editMode } =
    useMyVenue();

  const draft = useVenueStore((state) => state.draft);
  const setName = useVenueStore((state) => state.setName);
  const setAbout = useVenueStore((state) => state.setAbout);

  if (!venue || isLoading) return <DetailsPageSkeleton sections={5} />;

  const display = draft ?? venue;

  const hero = <VenueHero venue={display} onNameChange={setName} />;
  const { about, location, concerts, reviews } = venueSections(display, {
    onAboutChange: setAbout,
  });

  const opportunities: DetailsSection = {
    id: "opportunities",
    label: "Opportunities",
    content: <MyOpportunitiesSection venueId={venue.id} />,
  };

  const sections = [about, location, concerts, opportunities, reviews];

  return (
    <div>
      <ConfigBar
        editMode={editMode}
        isDirty={isDirty}
        isSaving={isSaving}
        onToggleEdit={toggleEdit}
        onSave={() => save()}
        onCancel={resetDraft}
      />

      <EditableProvider editMode={editMode}>
        <DetailsLayout hero={hero} sections={sections} />
      </EditableProvider>
    </div>
  );
}
