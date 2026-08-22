import type { PaginationParams } from "@concertable/web/hooks/usePagination";

export const moderationKeys = {
  reports: ["moderation", "reports"] as const,
  reportsList: (params: PaginationParams) =>
    [...moderationKeys.reports, params] as const,
};
