import { useMutation, useQueryClient } from "@tanstack/react-query";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";
import type { DoorRevenueRequest } from "@concertable/shared/features/concerts/schemas/doorRevenueRequestSchema";
import { myConcertQueryKey } from "@b2b/features/concerts/hooks/useMyConcertQuery";

export function useDeclareDoorRevenue(id: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: DoorRevenueRequest) =>
      concertApi.declareDoorRevenue(id, request),
    onSuccess: () => {
      // The owner read (["concert","mine",id]) drops the declare action once DoorRevenue is set;
      // the dashboard KPI count of gigs awaiting a declaration drops too.
      queryClient.invalidateQueries({ queryKey: myConcertQueryKey(id) });
      queryClient.invalidateQueries({ queryKey: ["dashboard", "venue", "kpis"] });
    },
  });
}
