import { apiClient } from "@concertable/web/lib/apiClient";
import type { Pagination } from "@concertable/web/types/common";
import type { PaginationParams } from "@concertable/web/hooks/usePagination";
import type { ContentReport } from "../types";
import type { ResolveReportRequest } from "../schemas/resolveReportRequestSchema";

const BASE = "/Moderation";

const moderationApi = {
  getReports: async (
    params: PaginationParams,
  ): Promise<Pagination<ContentReport>> => {
    const { data } = await apiClient.get<Pagination<ContentReport>>(
      `${BASE}/reports`,
      { params },
    );
    return data;
  },

  hideMessage: async (id: number): Promise<void> => {
    await apiClient.post(`${BASE}/messages/${id}/hide`);
  },

  restoreMessage: async (id: number): Promise<void> => {
    await apiClient.post(`${BASE}/messages/${id}/restore`);
  },

  resolveReport: async (
    id: number,
    request: ResolveReportRequest,
  ): Promise<void> => {
    await apiClient.post(`${BASE}/reports/${id}/resolve`, request);
  },
};

export default moderationApi;
