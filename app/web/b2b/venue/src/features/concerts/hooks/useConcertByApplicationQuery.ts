import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@concertable/shared/lib/apiClient";
import type { MyConcert } from "@concertable/b2b/features/concerts";

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
    queryKey: ["concert", "venue", "application", applicationId],
    queryFn: () => getConcertByApplication(applicationId),
    refetchInterval: (query) => (query.state.data ? false : 1_000),
  });
}
