import { useCreateArtist as useCreateArtistShared } from "@concertable/shared/features/artists";
import { useNavigate } from "@tanstack/react-router";

export function useCreateArtist() {
  const navigate = useNavigate();
  return useCreateArtistShared({
    onSuccess: () => void navigate({ to: "/" }),
  });
}
