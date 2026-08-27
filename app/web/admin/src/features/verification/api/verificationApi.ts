import { apiClient } from "@concertable/web/lib/apiClient";
import type { Pagination } from "@concertable/web/types/common";
import type { PaginationParams } from "@concertable/web/hooks/usePagination";
import type { PendingVerification } from "../types";
import type { RejectVerificationRequest } from "../schemas/rejectVerificationRequestSchema";

const BASE = "/verification";

const verificationApi = {
  getPending: async (
    params: PaginationParams,
  ): Promise<Pagination<PendingVerification>> => {
    const { data } = await apiClient.get<Pagination<PendingVerification>>(
      `${BASE}/pending`,
      { params },
    );
    return data;
  },

  approve: async (tenantId: string): Promise<void> => {
    await apiClient.post(`${BASE}/${tenantId}/approve`);
  },

  reject: async (
    tenantId: string,
    request: RejectVerificationRequest,
  ): Promise<void> => {
    await apiClient.post(`${BASE}/${tenantId}/reject`, request);
  },
};

export default verificationApi;
