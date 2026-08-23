import type { ActionLink, Genre } from "@concertable/shared/types/common";
import type { ArtistSummary } from "@concertable/shared/features/artists/types";
import type { Concert } from "@concertable/shared/features/concerts/types";
import type { Deal } from "@b2b/features/deals";

export type ApplicationStatus =
  | "pending"
  | "rejected"
  | "withdrawn"
  | "accepted"
  | "cancelled"
  | "awaitingPayment"
  | "confirmed"
  | "complete"
  | "settled";

export interface OpportunityActions {
  checkout?: ActionLink;
}

export interface OpportunityDraft {
  startDate: string;
  endDate: string;
  genres: Genre[];
  deal: Deal;
}

export interface Opportunity extends OpportunityDraft {
  id: number;
  venueId: number;
  actions: OpportunityActions;
}

export interface OpportunityRequest extends OpportunityDraft {
  id?: number;
}

export const Opportunity = {
  toRequest(opportunity: Opportunity | OpportunityDraft): OpportunityRequest {
    return {
      id: "id" in opportunity ? opportunity.id : undefined,
      startDate: opportunity.startDate,
      endDate: opportunity.endDate,
      genres: opportunity.genres,
      deal: opportunity.deal,
    };
  },
};

export interface ApplicationActions {
  accept: ActionLink;
  checkout?: ActionLink;
  withdraw?: ActionLink;
  reject?: ActionLink;
  cancel?: ActionLink;
  contract?: ActionLink;
}

export interface ConcertActions {
  cancel?: ActionLink;
  contract?: ActionLink;
  declareDoorRevenue?: ActionLink;
  invoice?: ActionLink;
}

export interface MyConcert extends Concert {
  ticketsSold: number;
  doorRevenue?: number;
  actions: ConcertActions;
}

export interface Application {
  id: number;
  artist: ArtistSummary;
  opportunity: Opportunity;
  status: ApplicationStatus;
  actions: ApplicationActions;
}
