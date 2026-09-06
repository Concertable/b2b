import { useCreateArtist as useCreateArtistShared } from "@concertable/b2b/features/artists";
import { useNavigate } from "@tanstack/react-router";

export function useCreateArtist() {
  const navigate = useNavigate();
  return useCreateArtistShared({
    onSuccess: () => void navigate({ to: "/" }),
  });
}
