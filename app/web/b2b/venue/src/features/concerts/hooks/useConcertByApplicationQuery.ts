import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@concertable/shared/lib/apiClient";
import type { MyConcert } from "@concertable/b2b/features/concerts";

export const venueConcertKeys = {
  all: ["concerts", "venue"] as const,
  byApplication: (applicationId: number) =>
    [...venueConcertKeys.all, "application", applicationId] as const,
};

async function getConcertByApplication(
  applicationId: number,
): Promise<MyConcert | null> {
  const { data } = await apiClient.getOptional<MyConcert>(
    `/concert/application/${applicationId}`,
  );
  return data;
}

export function useConcertByApplicationQuery(applicationId: number) {
  return useQuery({
    queryKey: venueConcertKeys.byApplication(applicationId),
    queryFn: () => getConcertByApplication(applicationId),
    refetchInterval: (query) => (query.state.data ? false : 1_000),
  });
}
