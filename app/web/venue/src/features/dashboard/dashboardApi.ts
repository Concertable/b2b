import { selectPersona } from "@concertable/shared/features/dashboard";
import type {
  ActivityItem,
  ConcertCard,
  MessageThread,
  MonthlyRevenuePoint,
  OpportunityWithCounts,
  ReviewExcerpt,
  Settlement,
} from "@concertable/shared/features/dashboard";
import { venueFixtures } from "./fixtures";
import type {
  Application,
  VenueDashboardKpis,
  VenueDashboardOverview,
} from "./types";

const dashboardApi = {
  getOverview: async (): Promise<VenueDashboardOverview> => {
    return venueFixtures[selectPersona()].overview;
  },
  getKpis: async (): Promise<VenueDashboardKpis> => {
    return venueFixtures[selectPersona()].kpis;
  },
  getApplicationsToReview: async (): Promise<Application[]> => {
    return venueFixtures[selectPersona()].applicationsToReview;
  },
  getInbox: async (): Promise<MessageThread[]> => {
    return venueFixtures[selectPersona()].inbox;
  },
  getUpcomingConcerts: async (): Promise<ConcertCard[]> => {
    return venueFixtures[selectPersona()].upcomingConcerts;
  },
  getTicketRevenue: async (): Promise<MonthlyRevenuePoint[]> => {
    return venueFixtures[selectPersona()].ticketRevenue;
  },
  getOpenOpportunities: async (): Promise<OpportunityWithCounts[]> => {
    return venueFixtures[selectPersona()].openOpportunities;
  },
  getActivity: async (): Promise<ActivityItem[]> => {
    return venueFixtures[selectPersona()].activity;
  },
  getSettlements: async (): Promise<Settlement[]> => {
    return venueFixtures[selectPersona()].settlements;
  },
  getRecentReviews: async (): Promise<ReviewExcerpt[]> => {
    return venueFixtures[selectPersona()].recentReviews;
  },
};

export default dashboardApi;
