import { ConfigBar } from "@concertable/web/components/ConfigBar";
import { EditableProvider } from "@concertable/shared/providers";
import { DetailsLayout } from "@concertable/web/components/details/DetailsLayout";
import { DetailsPageSkeleton } from "@concertable/web/components/skeletons/DetailsPageSkeleton";
import { ArtistHero, artistSections } from "@concertable/web/features/artists";
import { useMyArtist } from "../hooks/useMyArtist";

export function MyArtistPage() {
  const {
    artist,
    draft,
    isDirty,
    isSaving,
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
  } = useMyArtist();

  if (!artist) return <DetailsPageSkeleton sections={5} />;

  const display = draft ?? artist;

  const hero = (
    <ArtistHero
      artist={display}
      onNameChange={setName}
      onBannerChange={setBanner}
      onAvatarChange={setAvatar}
    />
  );
  const { about, location, concerts, reviews } = artistSections(display, {
    onAboutChange: setAbout,
  });
  const sections = [about, location, concerts, reviews];

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
