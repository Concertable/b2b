import { create } from "zustand";
import { immer } from "zustand/middleware/immer";
import { defaultDeal } from "@b2b/features/deals";
import type { Deal, PaymentMethod } from "@b2b/features/deals";
import type { Genre } from "@concertable/shared/types/common";
import type { Opportunity, OpportunityDraft } from "../types";

interface OpportunitiesStore {
  opportunities: Opportunity[];
  drafts: OpportunityDraft[];
  isDirty: boolean;

  hydrate: (opportunities: Opportunity[]) => void;
  reset: () => void;
  addDraft: (draft: OpportunityDraft) => void;

  removeOpportunity: (index: number) => void;
  removeDraft: (index: number) => void;

  setOpportunityDates: (index: number, start: string, end: string) => void;
  setOpportunityDealType: (index: number, type: Deal["$type"]) => void;
  setOpportunityDeal: (index: number, deal: Deal) => void;
  setOpportunityPaymentMethod: (index: number, method: PaymentMethod) => void;
  toggleOpportunityGenre: (index: number, genre: Genre) => void;

  setDraftDates: (index: number, start: string, end: string) => void;
  setDraftDealType: (index: number, type: Deal["$type"]) => void;
  setDraftDeal: (index: number, deal: Deal) => void;
  setDraftPaymentMethod: (index: number, method: PaymentMethod) => void;
  toggleDraftGenre: (index: number, genre: Genre) => void;
}

export const useOpportunitiesStore = create<OpportunitiesStore>()(
  immer((set) => ({
    opportunities: [],
    drafts: [],
    isDirty: false,

    hydrate: (opportunities) => set((s) => { s.opportunities = opportunities; s.drafts = []; s.isDirty = false; }),
    reset: () => set((s) => { s.opportunities = []; s.drafts = []; s.isDirty = false; }),
    addDraft: (draft) => set((s) => { s.drafts.push(draft); s.isDirty = true; }),

    removeOpportunity: (index) => set((s) => { s.opportunities.splice(index, 1); s.isDirty = true; }),
    removeDraft: (index) => set((s) => { s.drafts.splice(index, 1); s.isDirty = true; }),

    setOpportunityDates: (index, start, end) => set((s) => { s.opportunities[index].startDate = start; s.opportunities[index].endDate = end; s.isDirty = true; }),
    setOpportunityDealType: (index, type) => set((s) => { s.opportunities[index].deal = defaultDeal(type, s.opportunities[index].deal.paymentMethod); s.isDirty = true; }),
    setOpportunityDeal: (index, deal) => set((s) => { s.opportunities[index].deal = deal; s.isDirty = true; }),
    setOpportunityPaymentMethod: (index, method) => set((s) => { s.opportunities[index].deal.paymentMethod = method; s.isDirty = true; }),
    toggleOpportunityGenre: (index, genre) => set((s) => {
      const genres = s.opportunities[index].genres;
      const i = genres.indexOf(genre);
      if (i >= 0) genres.splice(i, 1); else genres.push(genre);
      s.isDirty = true;
    }),

    setDraftDates: (index, start, end) => set((s) => { s.drafts[index].startDate = start; s.drafts[index].endDate = end; s.isDirty = true; }),
    setDraftDealType: (index, type) => set((s) => { s.drafts[index].deal = defaultDeal(type, s.drafts[index].deal.paymentMethod); s.isDirty = true; }),
    setDraftDeal: (index, deal) => set((s) => { s.drafts[index].deal = deal; s.isDirty = true; }),
    setDraftPaymentMethod: (index, method) => set((s) => { s.drafts[index].deal.paymentMethod = method; s.isDirty = true; }),
    toggleDraftGenre: (index, genre) => set((s) => {
      const genres = s.drafts[index].genres;
      const i = genres.indexOf(genre);
      if (i >= 0) genres.splice(i, 1); else genres.push(genre);
      s.isDirty = true;
    }),
  }))
);
