import { useQuery } from "@tanstack/react-query";
import artistApi from "../api/artistApi";

export const artistKeys = {
  all: () => ["artist"] as const,
  details: () => ["artist", "details"] as const,
};

export function useArtistQuery() {
  return useQuery({
    queryKey: artistKeys.details(),
    queryFn: artistApi.getArtist,
    meta: { expectedErrors: [404] },
  });
}
