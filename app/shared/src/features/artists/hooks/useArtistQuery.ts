import { useQuery } from "@tanstack/react-query";
import artistApi from "../api/artistApi";

export const artistKeys = {
  all: () => ["artist"] as const,
  my: () => ["artist", "my"] as const,
  myForTenant: (tenantId: string | undefined) =>
    ["artist", "my", tenantId] as const,
  byId: (id: number) => ["artist", id] as const,
};

export function useArtistQuery(tenantId?: string) {
  return useQuery({
    queryKey: artistKeys.myForTenant(tenantId),
    queryFn: artistApi.getArtist,
    enabled: tenantId !== undefined,
    meta: { expectedErrors: [404] },
  });
}
