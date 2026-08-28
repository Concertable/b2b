import { useQuery, keepPreviousData } from "@tanstack/react-query";
import type { PaginationParams } from "@concertable/web/hooks/usePagination";
import verificationApi from "../api/verificationApi";
import { verificationKeys } from "./verificationKeys";

export function usePendingVerificationsQuery(params: PaginationParams) {
  return useQuery({
    queryKey: verificationKeys.pendingList(params),
    queryFn: () => verificationApi.getPending(params),
    placeholderData: keepPreviousData,
  });
}
