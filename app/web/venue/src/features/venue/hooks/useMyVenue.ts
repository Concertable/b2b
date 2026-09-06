import {
  useMyVenue as useMyVenueShared,
  useVenueQuery,
} from "@concertable/b2b/features/venues";
import { useOpportunities } from "@concertable/web-b2b/features/concerts/hooks/useOpportunities";
import { opportunitiesQueryKey } from "@concertable/web-b2b/features/concerts/hooks/useOpportunitiesQuery";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import type { Opportunity } from "@concertable/web-b2b/features/concerts/types";

export function useMyVenue() {
  const queryClient = useQueryClient();
  const venueQuery = useVenueQuery();
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
