import { useQuery, keepPreviousData } from "@tanstack/react-query";
import type { PaginationParams } from "@concertable/web/hooks/usePagination";
import venuesApi from "../api/venuesApi";
import { venuesKeys } from "./venuesKeys";

export function usePendingVenuesQuery(params: PaginationParams) {
  return useQuery({
    queryKey: venuesKeys.pendingApprovalList(params),
    queryFn: () => venuesApi.getPendingApproval(params),
    placeholderData: keepPreviousData,
  });
}
