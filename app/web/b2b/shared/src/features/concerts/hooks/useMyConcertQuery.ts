import { useQuery } from "@tanstack/react-query";
import myConcertApi from "../api/myConcertApi";

// Distinct from the public ["concert", id] key: the owner read carries action links the public
// read doesn't, so the two must not share a cache entry within a manager app that browses both.
export const myConcertQueryKey = (id: number) => ["concert", "mine", id];

export function useMyConcertQuery(id: number) {
  return useQuery({
    queryKey: myConcertQueryKey(id),
    queryFn: () => myConcertApi.getMyConcert(id),
  });
}
