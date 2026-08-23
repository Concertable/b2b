import { useCreateVenue as useCreateVenueShared } from "@concertable/shared/features/venues";
import { useNavigate } from "@tanstack/react-router";

export function useCreateVenue() {
  const navigate = useNavigate();
  return useCreateVenueShared({
    onSuccess: () => void navigate({ to: "/" }),
  });
}
