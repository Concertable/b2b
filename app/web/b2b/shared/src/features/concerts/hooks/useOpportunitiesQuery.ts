import { useQuery, keepPreviousData } from "@tanstack/react-query";
import type { PaginationParams } from "@concertable/shared/hooks/usePagination";
import opportunityApi from "../api/opportunityApi";

export function useOpportunitiesQuery(
  venueId: number,
  params: PaginationParams,
) {
  return useQuery({
    queryKey: ["opportunities", "venue", venueId, params],
    queryFn: () => opportunityApi.getPaged(venueId, params),
    placeholderData: keepPreviousData,
  });
}

export const opportunitiesQueryKey = (venueId: number) =>
  ["opportunities", "venue", venueId, "all"] as const;

export function useAllOpportunitiesQuery(venueId: number, enabled = true) {
  return useQuery({
    queryKey: opportunitiesQueryKey(venueId),
    queryFn: () => opportunityApi.getAll(venueId),
    enabled,
  });
}
