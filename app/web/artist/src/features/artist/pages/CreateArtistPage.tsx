import { EditableProvider } from "@concertable/shared/providers";
import { CreateBar } from "@concertable/web/components/CreateBar";
import { DetailsLayout } from "@concertable/web/components/details/DetailsLayout";
import {
  ArtistHero,
  artistSections,
} from "@concertable/web/features/artists";
import { useCreateArtist } from "../hooks/useCreateArtist";

export function CreateArtistPage() {
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
  } = useCreateArtist();

  const hero = (
    <ArtistHero
      artist={draft}
      onNameChange={setName}
      onBannerChange={setBanner}
      onAvatarChange={setAvatar}
    />
  );
  const { about, location, concerts } = artistSections(draft, {
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
