import type {
  ActivityItem,
  ConcertCard,
  MessageThread,
  MonthlyRevenuePoint,
  ReviewExcerpt,
} from "@concertable/shared/features/dashboard";
import type { OpportunityCard } from "@concertable/b2b/features/dashboard";
import type {
  Application,
  ArtistDashboardKpis,
  ArtistDashboardOverview,
} from "../types";

export interface ArtistDashboardFixture {
  overview: ArtistDashboardOverview;
  kpis: ArtistDashboardKpis;
  applications: Application[];
  inbox: MessageThread[];
  upcomingConcerts: ConcertCard[];
  payouts: MonthlyRevenuePoint[];
  recommendedOpportunities: OpportunityCard[];
  activity: ActivityItem[];
  recentReviews: ReviewExcerpt[];
}
