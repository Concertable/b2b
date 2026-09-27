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

export type ApplicationActionName =
  | "accept"
  | "checkout"
  | "decline"
  | "cancel"
  | "withdraw"
  | "contract";

export type ApplicationActionsOf<TName extends ApplicationActionName> = {
  [K in TName]?: ActionLink;
};

export type ApplicationActions = ApplicationActionsOf<ApplicationActionName>;

export interface ConcertOperationsActions {
  cancel?: ActionLink;
}

export interface ConcertFinanceActions {
  declareDoorRevenue?: ActionLink;
  invoice?: ActionLink;
}

export interface ConcertOperations extends Concert {
  applicationId: number;
  actions: ConcertOperationsActions;
}

export interface ConcertFinance {
  id: number;
  ticketsSold: number;
  doorRevenue?: number;
  isRevenueShare: boolean;
  actions: ConcertFinanceActions;
}

export interface ConcertDraftReference {
  id: number;
  applicationId: number;
}

export interface ApplicationProposal {
  id: number;
  artist: ArtistSummary;
  opportunity: {
    id: number;
    venueId: number;
    venueName: string;
    startDate: string;
    endDate: string;
    genres: Genre[];
    deal: Deal;
  };
  status: ApplicationStatus;
  actions: ApplicationActions;
}

export interface ApplicationSummary {
  id: number;
  artist: ArtistSummary;
  opportunity: {
    id: number;
    venueId: number;
    venueName: string;
    startDate: string;
    endDate: string;
    genres: Genre[];
  };
  status: ApplicationStatus;
}
