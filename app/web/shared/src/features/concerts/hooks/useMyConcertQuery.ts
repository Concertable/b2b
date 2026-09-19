import { useQuery } from "@tanstack/react-query";
import myConcertApi from "../api/myConcertApi";

export const myConcertKeys = {
  all: ["concert", "private"] as const,
  operations: (id: number) => [...myConcertKeys.all, id, "operations"] as const,
  finance: (id: number) => [...myConcertKeys.all, id, "finance"] as const,
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
