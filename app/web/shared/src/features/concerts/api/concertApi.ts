import { apiClient } from "@concertable/shared/lib/apiClient";
import type { ConcertFinance, ConcertOperations } from "../types";

const concertApi = {
  getOperations: async (id: number): Promise<ConcertOperations> => {
    const { data } = await apiClient.get<ConcertOperations>(
      `/concert/${id}/operations`,
    );
    return data;
  },

  getFinance: async (id: number): Promise<ConcertFinance> => {
    const { data } = await apiClient.get<ConcertFinance>(
      `/concert/${id}/finance`,
    );
    return data;
  },
};

export default concertApi;
