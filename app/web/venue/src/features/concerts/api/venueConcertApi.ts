import { apiClient } from "@concertable/shared/lib/apiClient";
import type { ConcertDraftReference } from "@concertable/web-b2b/features/concerts/types";

const venueConcertApi = {
  getDraftByApplication: async (
    applicationId: number,
  ): Promise<ConcertDraftReference | null> => {
    const { data } = await apiClient.get<ConcertDraftReference[]>(
      "/concert/drafts/current",
    );
    return data.find((concert) => concert.applicationId === applicationId) ?? null;
  },
};

export default venueConcertApi;
