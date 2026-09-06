import { useQuery } from "@tanstack/react-query";
import venueApi from "../api/venueApi";

export const venueKeys = {
  all: () => ["venue"] as const,
  my: () => ["venue", "my"] as const,
  byId: (id: number) => ["venue", id] as const,
};

export function useVenueQuery() {
  return useQuery({
    queryKey: venueKeys.my(),
    queryFn: venueApi.getVenue,
    meta: { expectedErrors: [404] },
  });
}
