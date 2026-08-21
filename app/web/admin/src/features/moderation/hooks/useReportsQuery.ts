import { useQuery, keepPreviousData } from "@tanstack/react-query";
import type { PaginationParams } from "@concertable/web/hooks/usePagination";
import moderationApi from "../api/moderationApi";
import { moderationKeys } from "./moderationKeys";

export function useReportsQuery(params: PaginationParams) {
  return useQuery({
    queryKey: moderationKeys.reportsList(params),
    queryFn: () => moderationApi.getReports(params),
    placeholderData: keepPreviousData,
  });
}
