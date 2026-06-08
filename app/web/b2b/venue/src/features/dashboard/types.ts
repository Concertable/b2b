import type {
  DashboardApplicationStatus,
  OpportunitySummary,
  ProfileHealth,
  ReviewSummary,
  StripeConnectStatus,
} from "@concertable/shared/features/dashboard";
import type { ArtistSummary } from "@concertable/shared/features/artists";
import type { ApplicationActions } from "./applicationActions";

export interface VenueDashboardOverview {
  venueId: number;
  venueName: string;
  profileHealth: ProfileHealth;
  stripeConnect: StripeConnectStatus;
  reviewSummary: ReviewSummary;
}

export interface VenueDashboardKpis {
  applicationsToReview: number;
  applicationsToReviewDelta?: number;
  openOpportunities: number;
  upcomingConcerts: number;
  mtdRevenueCents: number;
  mtdRevenueDeltaPercent?: number;
}

export interface Application {
  id: number;
  status: DashboardApplicationStatus;
  artist: ArtistSummary;
  opportunity: OpportunitySummary;
  actions: ApplicationActions;
}
