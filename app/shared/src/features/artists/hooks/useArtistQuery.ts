import { useQuery } from "@tanstack/react-query";
import artistApi from "../api/artistApi";

export const artistKeys = {
  all: () => ["artist"] as const,
  my: () => ["artist", "my"] as const,
  byId: (id: number) => ["artist", id] as const,
};

export function useArtistQuery() {
  return useQuery({
    queryKey: artistKeys.my(),
    queryFn: artistApi.getArtist,
    meta: { expectedErrors: [404] },
  });
}
