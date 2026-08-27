import type {
  DashboardApplicationStatus,
  ProfileHealth,
  ReviewSummary,
  StripeConnectStatus,
} from "@concertable/shared/features/dashboard/types";
import type { OpportunitySummary } from "@concertable/web-b2b/features/dashboard/types";
import type { Deal } from "@concertable/web-b2b/features/deals/types";
import type { Genre } from "@concertable/shared/types/common";
import type { ApplicationActions } from "./applicationActions";

export interface ArtistDashboardOverview {
  artistId: number;
  artistName: string;
  profileHealth: ProfileHealth;
  stripeConnect: StripeConnectStatus;
  reviewSummary: ReviewSummary;
}

export interface ArtistDashboardKpis {
  pendingApplications: number;
  acceptedAwaitingCheckout: number;
  upcomingConcerts: number;
  mtdPayoutsCents: number;
}

export interface Application {
  id: number;
  status: DashboardApplicationStatus;
  opportunity: OpportunitySummary;
  actions: ApplicationActions;
}

export interface OpportunityMatch {
  id: number;
  venueId: number;
  venueName: string;
  county: string;
  town: string;
  startDate: string;
  endDate: string;
  genres: Genre[];
  deal: Deal;
  fitScore: number;
  href: string;
}
