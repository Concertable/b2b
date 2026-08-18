import {
  useMyVenue as useMyVenueShared,
  useMyVenueQuery,
} from "@concertable/shared/features/venues";
import type { UseMyVenueResult } from "@concertable/shared/features/venues";
import { useOpportunities } from "@concertable/web-b2b/features/concerts/hooks/useOpportunities";
import { opportunitiesQueryKey } from "@concertable/web-b2b/features/concerts/hooks/useOpportunitiesQuery";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import type { Opportunity } from "@concertable/web-b2b/features/concerts/types";

export function useMyVenue(): UseMyVenueResult {
  const queryClient = useQueryClient();
  const venueQuery = useMyVenueQuery();
  const venueId = venueQuery.data?.id ?? 0;

  const {
    save: saveOpportunities,
    hydrate: hydrateOpportunities,
    reset: resetOpportunities,
    isDirty: opportunitiesIsDirty,
    isSuccess: opportunitiesLoaded,
  } = useOpportunities(venueId);

  const result = useMyVenueShared({
    onSuccess: () => {
      resetOpportunities();
      toast.success("Venue saved!");
    },
    afterSave: () => saveOpportunities(),
    onToggleEdit: () => {
      const cached =
        queryClient.getQueryData<Opportunity[]>(
          opportunitiesQueryKey(venueId),
        ) ?? [];
      hydrateOpportunities(cached);
    },
    onResetDraft: () => resetOpportunities(),
    extraDirty: opportunitiesIsDirty,
  });

  return { ...result, isLoading: result.isLoading || !opportunitiesLoaded };
}
