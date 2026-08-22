import { apiClient } from "@concertable/web/lib/apiClient";
import type { Pagination } from "@concertable/web/types/common";
import type { PaginationParams } from "@concertable/web/hooks/usePagination";
import type { PendingVenue } from "../types";

const BASE = "/venue";

const venuesApi = {
  getPendingApproval: async (
    params: PaginationParams,
  ): Promise<Pagination<PendingVenue>> => {
    const { data } = await apiClient.get<Pagination<PendingVenue>>(
      `${BASE}/pending-approval`,
      { params },
    );
    return data;
  },

  approve: async (id: number): Promise<void> => {
    await apiClient.patch(`${BASE}/${id}/approve`);
  },
};

export default venuesApi;
