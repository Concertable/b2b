import type { PaginationParams } from "@concertable/web/hooks/usePagination";

export const verificationKeys = {
  pending: ["verification", "pending"] as const,
  pendingList: (params: PaginationParams) =>
    [...verificationKeys.pending, params] as const,
};
