import type {
  ActivityItem,
  ConcertCard,
  MessageThread,
  MonthlyRevenuePoint,
  ReviewExcerpt,
  Settlement,
} from "@concertable/shared/features/dashboard";
import type { OpportunityWithCounts } from "@b2b/features/dashboard";
import type {
  Application,
  VenueDashboardKpis,
  VenueDashboardOverview,
} from "../types";

export interface VenueDashboardFixture {
  overview: VenueDashboardOverview;
  kpis: VenueDashboardKpis;
  applicationsToReview: Application[];
  inbox: MessageThread[];
  upcomingConcerts: ConcertCard[];
  ticketRevenue: MonthlyRevenuePoint[];
  openOpportunities: OpportunityWithCounts[];
  activity: ActivityItem[];
  settlements: Settlement[];
  recentReviews: ReviewExcerpt[];
}
