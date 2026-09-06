import { useMyArtist as useMyArtistShared } from "@concertable/b2b/features/artists";
import { toast } from "sonner";

export function useMyArtist() {
  return useMyArtistShared({
    onSuccess: () => toast.success("Artist saved!"),
  });
}
