import { apiClient } from "@concertable/shared/lib/apiClient";
import type { MyConcert } from "../types";

const myConcertApi = {
  getMyConcert: async (id: number): Promise<MyConcert> => {
    const { data } = await apiClient.get<MyConcert>(
      `/organization/concert/${id}`,
    );
    return data;
  },
};

export default myConcertApi;
