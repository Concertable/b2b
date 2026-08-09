import { apiClient } from "@concertable/shared/lib/apiClient";
import type { MyConcert } from "@concertable/b2b/features/concerts";

const venueConcertApi = {
  getByApplication: async (
    applicationId: number,
  ): Promise<MyConcert | null> => {
    const { data } = await apiClient.getOptional<MyConcert>(
      `/concert/application/${applicationId}`,
    );
    return data;
  },
};

export default venueConcertApi;
