import { ConfigBar } from "@concertable/web/components/ConfigBar";
import { EditableProvider } from "@concertable/shared/providers";
import { DetailsLayout, type DetailsSection } from "@concertable/web/components/details/DetailsLayout";
import { DetailsPageSkeleton } from "@concertable/web/components/skeletons/DetailsPageSkeleton";
import { VenueHero, venueSections } from "@concertable/web/features/venues";
import { useMyVenue } from "../hooks/useMyVenue";
import { MyOpportunitiesSection } from "../components/MyOpportunitiesSection";

export function MyVenuePage() {
  const {
    venue,
    draft,
    isDirty,
    isSaving,
    isLoading,
    canSave,
    saveError,
    save,
    resetDraft,
    toggleEdit,
    editMode,
    setName,
    setAbout,
    setBanner,
    setAvatar,
  } = useMyVenue();

  if (!venue || isLoading) return <DetailsPageSkeleton sections={5} />;

  const display = draft ?? venue;

  const hero = (
    <VenueHero
      venue={display}
      onNameChange={setName}
      onBannerChange={setBanner}
      onAvatarChange={setAvatar}
    />
  );
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
        canSave={canSave}
        error={saveError}
        onToggleEdit={toggleEdit}
        onSave={save}
        onCancel={resetDraft}
      />

      <EditableProvider editMode={editMode}>
        <DetailsLayout hero={hero} sections={sections} />
      </EditableProvider>
    </div>
  );
}
