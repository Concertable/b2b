import type { ActionLink, Genre } from "@concertable/shared/types/common";
import type { ArtistSummary } from "@concertable/shared/features/artists/types";
import type { Concert } from "@concertable/shared/features/concerts/types";
import type { Deal } from "@b2b/features/deals";

export type ApplicationStatus =
  | "Pending"
  | "Rejected"
  | "Withdrawn"
  | "Accepted"
  | "Cancelled"
  | "AwaitingPayment"
  | "Confirmed"
  | "Complete"
  | "Settled";

export interface OpportunityActions {
  checkout?: ActionLink | null;
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

export interface ApplicationActions {
  accept: ActionLink;
  checkout?: ActionLink | null;
  withdraw?: ActionLink | null;
  reject?: ActionLink | null;
  cancel?: ActionLink | null;
  contract?: ActionLink | null;
}

export interface ConcertActions {
  cancel?: ActionLink | null;
  contract?: ActionLink | null;
  declareDoorRevenue?: ActionLink | null;
  invoice?: ActionLink | null;
}

// The party-scoped owner read (GET /concert/user/{id}): the public concert plus the venue-private
// figures and party-only action links the anonymous marketplace read omits.
export interface MyConcert extends Concert {
  ticketsSold: number;
  doorRevenue: number | null;
  actions: ConcertActions;
}

export interface Application {
  id: number;
  artist: ArtistSummary;
  opportunity: Opportunity;
  status: ApplicationStatus;
  actions: ApplicationActions;
}
