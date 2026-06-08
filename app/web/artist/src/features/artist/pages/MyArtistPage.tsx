import { ConfigBar } from "@/components/ConfigBar";
import { EditableProvider } from "@concertable/shared/providers";
import { DetailsLayout } from "@/components/details/DetailsLayout";
import { DetailsPageSkeleton } from "@/components/skeletons/DetailsPageSkeleton";
import { useArtistStore, ArtistHero, artistSections } from "@/features/artists";
import { useMyArtist } from "../hooks/useMyArtist";

export function MyArtistPage() {
  const { artist, isDirty, isSaving, save, resetDraft, toggleEdit, editMode } =
    useMyArtist();

  const draft = useArtistStore((state) => state.draft);
  const setName = useArtistStore((state) => state.setName);
  const setAbout = useArtistStore((state) => state.setAbout);

  if (!artist) return <DetailsPageSkeleton sections={5} />;

  const display = draft ?? artist;

  const hero = <ArtistHero artist={display} onNameChange={setName} />;
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
