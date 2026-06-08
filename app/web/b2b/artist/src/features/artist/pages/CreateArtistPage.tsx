import { useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { EditableProvider } from "@concertable/shared/providers";
import artistApi from "@concertable/shared/features/artists/api/artistApi";
import type { Artist } from "@concertable/shared/features/artists/types";
import { CreateBar } from "@/components/CreateBar";
import { DetailsLayout } from "@/components/details/DetailsLayout";
import { useArtistStore, ArtistHero, artistSections } from "@/features/artists";

const blank: Artist = {
  id: 0,
  name: "",
  about: "",
  bannerUrl: "",
  rating: 0,
  genres: [],
  email: "",
  county: "",
  town: "",
  latitude: 51.5074,
  longitude: -0.1278,
};

export function CreateArtistPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const draft = useArtistStore((s) => s.draft);
  const banner = useArtistStore((s) => s.banner);
  const avatar = useArtistStore((s) => s.avatar);
  const setName = useArtistStore((s) => s.setName);
  const setAbout = useArtistStore((s) => s.setAbout);
  const resetDraft = useArtistStore((s) => s.resetDraft);
  const toggleEdit = useArtistStore((s) => s.toggleEdit);

  useEffect(() => {
    toggleEdit(blank);
    return () => resetDraft(blank);
  }, [toggleEdit, resetDraft]);

  const mutation = useMutation({
    mutationFn: () =>
      artistApi.createArtist({
        name: draft!.name,
        about: draft!.about,
        latitude: draft!.latitude,
        longitude: draft!.longitude,
        genres: draft!.genres,
        banner: banner! as unknown as File,
        avatar: avatar! as unknown as File,
      }),
    onSuccess: (saved) => {
      queryClient.setQueryData(["artist", "my"], saved);
      navigate({ to: "/" });
    },
  });

  const canSubmit = !!(draft?.name && draft.about && banner && avatar);

  if (!draft) return null;

  const hero = <ArtistHero artist={draft} onNameChange={setName} />;
  const { about, location, concerts } = artistSections(draft, {
    onAboutChange: setAbout,
  });
  const sections = [about, location, concerts];

  return (
    <div>
      <CreateBar
        isSaving={mutation.isPending}
        canSubmit={canSubmit}
        onCreate={() => mutation.mutate()}
      />
      <EditableProvider editMode>
        <DetailsLayout hero={hero} sections={sections} />
      </EditableProvider>
    </div>
  );
}
