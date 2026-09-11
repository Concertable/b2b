import { apiClient } from "@concertable/shared/lib/apiClient";
import type { Pagination } from "@concertable/shared/types/common";
import type { PaginationParams } from "@concertable/shared/hooks/usePagination";
import { Opportunity, type OpportunityDraft } from "../types";

const opportunityApi = {
  getPaged: async (
    venueId: number,
    params: PaginationParams,
  ): Promise<Pagination<Opportunity>> => {
    const { data } = await apiClient.get<Pagination<Opportunity>>(
      `/opportunity/active/venue/${venueId}`,
      { params },
    );
    return data;
  },

  getAll: async (venueId: number): Promise<Opportunity[]> => {
    const { data } = await apiClient.get<Opportunity[]>(
      `/venue/${venueId}/opportunities`,
    );
    return data;
  },

  update: async (
    venueId: number,
    desired: (Opportunity | OpportunityDraft)[],
  ): Promise<Opportunity[]> => {
    const { data } = await apiClient.put<Opportunity[]>(
      `/venue/${venueId}/opportunities`,
      desired.map(Opportunity.toRequest),
    );
    return data;
  },
};

export default opportunityApi;
