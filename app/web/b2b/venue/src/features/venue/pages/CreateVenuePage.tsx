import { EditableProvider } from "@concertable/shared/providers";
import { CreateBar } from "@concertable/web/components/CreateBar";
import { DetailsLayout } from "@concertable/web/components/details/DetailsLayout";
import {
  VenueHero,
  venueSections,
} from "@concertable/web/features/venues";
import { useCreateVenue } from "../hooks/useCreateVenue";

export function CreateVenuePage() {
  const {
    draft,
    isCreating,
    canCreate,
    createError,
    create,
    setName,
    setAbout,
    setBanner,
    setAvatar,
  } = useCreateVenue();

  const hero = (
    <VenueHero
      venue={draft}
      onNameChange={setName}
      onBannerChange={setBanner}
      onAvatarChange={setAvatar}
    />
  );
  const { about, location, concerts } = venueSections(draft, {
    onAboutChange: setAbout,
  });

  return (
    <div>
      <CreateBar
        isSaving={isCreating}
        canSubmit={canCreate}
        error={createError}
        onCreate={create}
      />
      <EditableProvider editMode>
        <DetailsLayout
          hero={hero}
          sections={[about, location, concerts]}
        />
      </EditableProvider>
    </div>
  );
}
