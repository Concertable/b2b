import { useQueryClient } from "@tanstack/react-query";
import opportunityApi from "../api/opportunityApi";
import { useOpportunitiesStore } from "../store/useOpportunitiesStore";
import {
  opportunitiesQueryKey,
  useAllOpportunitiesQuery,
} from "./useOpportunitiesQuery";

export function useOpportunities(venueId: number) {
  const enabled = venueId > 0;
  const { isLoading, isError, isSuccess } = useAllOpportunitiesQuery(venueId, enabled);
  const queryClient = useQueryClient();
  const queryKey = opportunitiesQueryKey(venueId);

  const opportunities = useOpportunitiesStore((s) => s.opportunities);
  const drafts = useOpportunitiesStore((s) => s.drafts);
  const isDirty = useOpportunitiesStore((s) => s.isDirty);
  const hydrate = useOpportunitiesStore((s) => s.hydrate);
  const reset = useOpportunitiesStore((s) => s.reset);
  const addDraft = useOpportunitiesStore((s) => s.addDraft);

  const removeOpportunity = useOpportunitiesStore((s) => s.removeOpportunity);
  const setOpportunityDates = useOpportunitiesStore((s) => s.setOpportunityDates);
  const setOpportunityDealType = useOpportunitiesStore((s) => s.setOpportunityDealType);
  const setOpportunityDeal = useOpportunitiesStore((s) => s.setOpportunityDeal);
  const setOpportunityPaymentMethod = useOpportunitiesStore((s) => s.setOpportunityPaymentMethod);
  const toggleOpportunityGenre = useOpportunitiesStore((s) => s.toggleOpportunityGenre);

  const removeDraft = useOpportunitiesStore((s) => s.removeDraft);
  const setDraftDates = useOpportunitiesStore((s) => s.setDraftDates);
  const setDraftDealType = useOpportunitiesStore((s) => s.setDraftDealType);
  const setDraftDeal = useOpportunitiesStore((s) => s.setDraftDeal);
  const setDraftPaymentMethod = useOpportunitiesStore((s) => s.setDraftPaymentMethod);
  const toggleDraftGenre = useOpportunitiesStore((s) => s.toggleDraftGenre);

  return {
    opportunities,
    drafts,
    isLoading,
    isError,
    isSuccess,
    isDirty,
    hydrate,
    reset,
    addDraft,
    opportunityActions: {
      remove: removeOpportunity,
      setDates: setOpportunityDates,
      setDealType: setOpportunityDealType,
      setDeal: setOpportunityDeal,
      setPaymentMethod: setOpportunityPaymentMethod,
      toggleGenre: toggleOpportunityGenre,
    },
    draftActions: {
      remove: removeDraft,
      setDates: setDraftDates,
      setDealType: setDraftDealType,
      setDeal: setDraftDeal,
      setPaymentMethod: setDraftPaymentMethod,
      toggleGenre: toggleDraftGenre,
    },
    save: async () => {
      const updated = await opportunityApi.update(venueId, [...opportunities, ...drafts]);
      hydrate(updated);
      await queryClient.invalidateQueries({ queryKey });
    },
  };
}
