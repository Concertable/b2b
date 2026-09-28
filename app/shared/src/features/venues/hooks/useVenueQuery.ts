import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "../../tenant/queryKeys";
import venueApi from "../api/venueApi";

export const venueKeys = {
  all: () => currentPrivateQueryKey("venue"),
  my: () => currentPrivateQueryKey("venue", "my"),
  myForTenant: (tenantId: string | undefined) =>
    currentPrivateQueryKey("venue", "my", tenantId),
  byId: (id: number) => ["venue", id] as const,
};

export function useVenueQuery(tenantId: string | undefined) {
  return useQuery({
    queryKey: venueKeys.myForTenant(tenantId),
    queryFn: venueApi.getVenue,
    enabled: tenantId !== undefined,
    meta: { expectedErrors: [404] },
  });
}
