import { useQuery } from "@tanstack/react-query";
import { concertKeys } from "../queryKeys";
import concertApi from "../api/concertApi";

export function useConcertOperationsQuery(id: number) {
  return useQuery({
    queryKey: concertKeys.operations(id),
    queryFn: () => concertApi.getOperations(id),
  });
}
