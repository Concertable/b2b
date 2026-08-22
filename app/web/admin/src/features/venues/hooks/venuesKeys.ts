import type { PaginationParams } from "@concertable/web/hooks/usePagination";

export const venuesKeys = {
  pendingApproval: ["venues", "pending-approval"] as const,
  pendingApprovalList: (params: PaginationParams) =>
    [...venuesKeys.pendingApproval, params] as const,
};
