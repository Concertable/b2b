import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";
import type {
  ConcertFinance,
  ConcertOperations,
} from "@concertable/web-b2b/features/concerts/types";
import {
  doorRevenueRequestSchema,
  type DoorRevenueRequest,
} from "@concertable/shared/features/concerts/schemas/doorRevenueRequestSchema";
import { myConcertKeys } from "@concertable/web-b2b/features/concerts";

export function useDeclareDoorRevenue(
  concert: ConcertOperations,
  finance: ConcertFinance,
  rawValue: string,
) {
  const queryClient = useQueryClient();

  const parsed = doorRevenueRequestSchema.safeParse({ doorRevenue: Number(rawValue) });
  const errorMessage = parsed.success
    ? undefined
    : parsed.error.issues[0].message;

  const external = Number(rawValue) || 0;
  const concertableSales = finance.ticketsSold * concert.price;
  const total = concertableSales + external;

  const mutation = useMutation({
    mutationFn: (request: DoorRevenueRequest) =>
      concertApi.declareDoorRevenue(concert.id, request),
    onSuccess: () => {
      toast.success("Door takings recorded. The artist's share will settle shortly.");
      queryClient.invalidateQueries({ queryKey: myConcertKeys.finance(concert.id) });
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
