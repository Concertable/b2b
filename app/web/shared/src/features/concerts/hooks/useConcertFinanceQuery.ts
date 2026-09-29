import { useQuery } from "@tanstack/react-query";
import { concertKeys } from "../queryKeys";
import concertApi from "../api/concertApi";

export function useConcertFinanceQuery(id: number) {
  return useQuery({
    queryKey: concertKeys.finance(id),
    queryFn: () => concertApi.getFinance(id),
  });
}
