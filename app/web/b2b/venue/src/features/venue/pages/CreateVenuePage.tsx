import { useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { EditableProvider } from "@concertable/shared/providers";
import venueApi from "@concertable/shared/features/venues/api/venueApi";
import type { Venue } from "@concertable/shared/features/venues/types";
import { CreateBar } from "@/components/CreateBar";
import { DetailsLayout } from "@/components/details/DetailsLayout";
import { useVenueStore, VenueHero, venueSections } from "@/features/venues";

const blank: Venue = {
  id: 0,
  name: "",
  about: "",
  bannerUrl: "",
  rating: 0,
  county: "",
  town: "",
  email: "",
  latitude: 51.5074,
  longitude: -0.1278,
};

export function CreateVenuePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const draft = useVenueStore((s) => s.draft);
  const banner = useVenueStore((s) => s.banner);
  const avatar = useVenueStore((s) => s.avatar);
  const setName = useVenueStore((s) => s.setName);
  const setAbout = useVenueStore((s) => s.setAbout);
  const resetDraft = useVenueStore((s) => s.resetDraft);
  const toggleEdit = useVenueStore((s) => s.toggleEdit);

  useEffect(() => {
    toggleEdit(blank);
    return () => resetDraft(blank);
  }, [toggleEdit, resetDraft]);

  const mutation = useMutation({
    mutationFn: () =>
      venueApi.createVenue({
        name: draft!.name,
        about: draft!.about,
        latitude: draft!.latitude,
        longitude: draft!.longitude,
        banner: banner! as unknown as File,
        avatar: avatar! as unknown as File,
      }),
    onSuccess: (saved) => {
      queryClient.setQueryData(["venue", "my"], saved);
      navigate({ to: "/" });
    },
  });

  const canSubmit = !!(draft?.name && draft.about && banner && avatar);

  if (!draft) return null;

  const hero = <VenueHero venue={draft} onNameChange={setName} />;
  const { about, location, concerts } = venueSections(draft, {
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
