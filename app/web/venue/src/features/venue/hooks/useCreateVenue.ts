import { useCreateVenue as useCreateVenueShared } from "@concertable/b2b/features/venues";
import { useNavigate } from "@tanstack/react-router";

export function useCreateVenue() {
  const navigate = useNavigate();
  return useCreateVenueShared({
    onSuccess: () => void navigate({ to: "/" }),
  });
}
