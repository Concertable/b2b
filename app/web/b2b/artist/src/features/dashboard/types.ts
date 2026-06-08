import type {
  DashboardApplicationStatus,
  OpportunitySummary,
  ProfileHealth,
  ReviewSummary,
  StripeConnectStatus,
} from "@concertable/shared/features/dashboard";
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
  mtdPayoutsDeltaPercent?: number;
}

export interface Application {
  id: number;
  status: DashboardApplicationStatus;
  opportunity: OpportunitySummary;
  actions: ApplicationActions;
}
