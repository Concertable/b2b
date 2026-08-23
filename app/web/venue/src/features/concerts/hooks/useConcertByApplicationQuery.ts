import { useQuery } from "@tanstack/react-query";
import venueConcertApi from "../api/venueConcertApi";

export const venueConcertKeys = {
  all: ["concerts", "venue"] as const,
  byApplication: (applicationId: number) =>
    [...venueConcertKeys.all, "application", applicationId] as const,
};

export function useConcertByApplicationQuery(applicationId: number) {
  return useQuery({
    queryKey: venueConcertKeys.byApplication(applicationId),
    queryFn: () => venueConcertApi.getByApplication(applicationId),
    refetchInterval: (query) =>
      query.state.status === "success" && query.state.data === undefined
        ? 1_000
        : false,
  });
}
