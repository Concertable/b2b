import { useMyArtist as useMyArtistShared } from "@concertable/shared/features/artists";
import { toast } from "sonner";

export function useMyArtist() {
  return useMyArtistShared({
    onSuccess: () => toast.success("Artist saved!"),
    onError: () => toast.error("Failed to save artist."),
  });
}
