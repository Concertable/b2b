import api from "@concertable/shared/lib/axiosClient";
import type { Pagination } from "@concertable/shared/types/common";
import type { PaginationParams } from "@concertable/shared/hooks/usePagination";
import type { Opportunity, OpportunityDraft } from "../types";

const opportunityApi = {
  getPaged: async (
    venueId: number,
    params: PaginationParams,
  ): Promise<Pagination<Opportunity>> => {
    const { data } = await api.get<Pagination<Opportunity>>(
      `/opportunity/active/venue/${venueId}`,
      { params },
    );
    return data;
  },

  getAll: async (venueId: number): Promise<Opportunity[]> => {
    const { data } = await api.get<Opportunity[]>(
      `/venue/${venueId}/opportunities`,
    );
    return data;
  },

  update: async (
    venueId: number,
    desired: (Opportunity | OpportunityDraft)[],
  ): Promise<Opportunity[]> => {
    const { data } = await api.put<Opportunity[]>(
      `/venue/${venueId}/opportunities`,
      desired.map((o) => ({
        id: "id" in o ? o.id : undefined,
        startDate: o.startDate,
        endDate: o.endDate,
        genres: o.genres,
        deal: o.deal,
      })),
    );
    return data;
  },
};

export default opportunityApi;
