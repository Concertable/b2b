import { selectPersona } from "@concertable/shared/features/dashboard";
import type {
  ActivityItem,
  ConcertCard,
  MessageThread,
  MonthlyRevenuePoint,
  OpportunityCard,
  ReviewExcerpt,
} from "@concertable/shared/features/dashboard";
import { artistFixtures } from "./fixtures";
import type {
  Application,
  ArtistDashboardKpis,
  ArtistDashboardOverview,
} from "./types";

const dashboardApi = {
  getOverview: async (): Promise<ArtistDashboardOverview> => {
    return artistFixtures[selectPersona()].overview;
  },
  getKpis: async (): Promise<ArtistDashboardKpis> => {
    return artistFixtures[selectPersona()].kpis;
  },
  getApplications: async (): Promise<Application[]> => {
    return artistFixtures[selectPersona()].applications;
  },
  getInbox: async (): Promise<MessageThread[]> => {
    return artistFixtures[selectPersona()].inbox;
  },
  getUpcomingConcerts: async (): Promise<ConcertCard[]> => {
    return artistFixtures[selectPersona()].upcomingConcerts;
  },
  getPayouts: async (): Promise<MonthlyRevenuePoint[]> => {
    return artistFixtures[selectPersona()].payouts;
  },
  getRecommendedOpportunities: async (): Promise<OpportunityCard[]> => {
    return artistFixtures[selectPersona()].recommendedOpportunities;
  },
  getActivity: async (): Promise<ActivityItem[]> => {
    return artistFixtures[selectPersona()].activity;
  },
  getRecentReviews: async (): Promise<ReviewExcerpt[]> => {
    return artistFixtures[selectPersona()].recentReviews;
  },
};

export default dashboardApi;
