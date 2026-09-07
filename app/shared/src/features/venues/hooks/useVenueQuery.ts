import { useQuery } from "@tanstack/react-query";
import venueApi from "../api/venueApi";

export const venueKeys = {
  all: () => ["venue"] as const,
  my: () => ["venue", "my"] as const,
  myForTenant: (tenantId: string | undefined) =>
    ["venue", "my", tenantId] as const,
  byId: (id: number) => ["venue", id] as const,
};

export function useVenueQuery(tenantId?: string) {
  return useQuery({
    queryKey: venueKeys.myForTenant(tenantId),
    queryFn: venueApi.getVenue,
    enabled: tenantId !== undefined,
    meta: { expectedErrors: [404] },
  });
}
