import { useQuery } from "@tanstack/react-query";
import venueApi from "../api/venueApi";

export const venueKeys = {
  all: () => ["venue"] as const,
  details: () => ["venue", "details"] as const,
};

export function useVenueQuery() {
  return useQuery({
    queryKey: venueKeys.details(),
    queryFn: venueApi.getVenue,
    meta: { expectedErrors: [404] },
  });
}
