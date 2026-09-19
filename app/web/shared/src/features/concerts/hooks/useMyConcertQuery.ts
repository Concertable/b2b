import { useQuery } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import myConcertApi from "../api/myConcertApi";

export const myConcertKeys = {
  all: () => currentPrivateQueryKey("concert"),
  operations: (id: number) => currentPrivateQueryKey("concert", id, "operations"),
  finance: (id: number) => currentPrivateQueryKey("concert", id, "finance"),
};

export function useMyConcertQuery(id: number) {
  return useQuery({
    queryKey: myConcertKeys.operations(id),
    queryFn: () => myConcertApi.getOperations(id),
  });
}

export function useConcertFinanceQuery(id: number) {
  return useQuery({
    queryKey: myConcertKeys.finance(id),
    queryFn: () => myConcertApi.getFinance(id),
  });
}
