import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "../../tenant/queryKeys";
import artistApi from "../api/artistApi";

export const artistKeys = {
  all: () => currentPrivateQueryKey("artist"),
  my: () => currentPrivateQueryKey("artist", "my"),
  myForTenant: (tenantId: string | undefined) =>
    currentPrivateQueryKey("artist", "my", tenantId),
  byId: (id: number) => ["artist", id] as const,
};

export function useArtistQuery(tenantId: string | undefined) {
  return useQuery({
    queryKey: artistKeys.myForTenant(tenantId),
    queryFn: artistApi.getArtist,
    enabled: tenantId !== undefined,
    meta: { expectedErrors: [404] },
  });
}
