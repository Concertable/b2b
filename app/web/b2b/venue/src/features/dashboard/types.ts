import type {
  DashboardApplicationStatus,
  ProfileHealth,
  ReviewSummary,
  StripeConnectStatus,
} from "@concertable/shared/features/dashboard/types";
import type { OpportunitySummary } from "@concertable/web-b2b/features/dashboard/types";
import type { ArtistSummary } from "@concertable/shared/features/artists/types";
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
  openOpportunities: number;
  upcomingConcerts: number;
  mtdRevenueCents: number;
}

export interface Application {
  id: number;
  status: DashboardApplicationStatus;
  artist: ArtistSummary;
  opportunity: OpportunitySummary;
  actions: ApplicationActions;
}

export interface OpportunityApplicationMetrics {
  opportunity: OpportunitySummary;
  applicationCount: number;
  daysUntilDeadline: number;
}
