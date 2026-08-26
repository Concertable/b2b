import { apiClient } from "@concertable/shared/lib/apiClient";
import type { MyConcert } from "@concertable/web-b2b/features/concerts/types";

const venueConcertApi = {
  getByApplication: async (
    applicationId: number,
  ): Promise<MyConcert | undefined> => {
    const { data } = await apiClient.getOptional<MyConcert>(
      `/concert/application/${applicationId}`,
    );
    return data;
  },
};

export default venueConcertApi;
