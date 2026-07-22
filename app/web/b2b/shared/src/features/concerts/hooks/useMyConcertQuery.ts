import { useQuery } from "@tanstack/react-query";
import { concertKeys } from "@concertable/shared/features/concerts/hooks/useConcertQuery";
import myConcertApi from "../api/myConcertApi";

export function useMyConcertQuery(id: number) {
  return useQuery({
    queryKey: concertKeys.my(id),
    queryFn: () => myConcertApi.getMyConcert(id),
  });
}
