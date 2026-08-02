import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";
import type { MyConcert } from "@b2b/features/concerts";
import {
  doorRevenueRequestSchema,
  type DoorRevenueRequest,
} from "@concertable/shared/features/concerts/schemas/doorRevenueRequestSchema";
import { concertKeys } from "@concertable/shared/features/concerts/hooks/useConcertQuery";

export function useDeclareDoorRevenue(concert: MyConcert, rawValue: string) {
  const queryClient = useQueryClient();

  const parsed = doorRevenueRequestSchema.safeParse({ doorRevenue: Number(rawValue) });
  const errorMessage = parsed.success ? null : parsed.error.issues[0].message;

  const external = Number(rawValue) || 0;
  const concertableSales = concert.ticketsSold * concert.price;
  const total = concertableSales + external;

  const mutation = useMutation({
    mutationFn: (request: DoorRevenueRequest) =>
      concertApi.declareDoorRevenue(concert.id, request),
    onSuccess: () => {
      toast.success("Door takings recorded. The artist's share will settle shortly.");
      // The owner read drops the declare action once DoorRevenue is set; the dashboard
      // KPI count of gigs awaiting a declaration drops too.
      queryClient.invalidateQueries({ queryKey: concertKeys.my(concert.id) });
      queryClient.invalidateQueries({ queryKey: ["dashboard", "venue", "kpis"] });
    },
  });

  const declare = (onDone: () => void) => {
    if (parsed.success) mutation.mutate(parsed.data, { onSuccess: onDone });
    return parsed;
  };

  return {
    errorMessage,
    concertableSales,
    external,
    total,
    declare,
    isPending: mutation.isPending,
  };
}
