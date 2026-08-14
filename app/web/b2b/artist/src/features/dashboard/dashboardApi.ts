import type {
  ActivityItem,
  ConcertCard,
  MessagePreview,
  MonthlyRevenuePoint,
  ReviewExcerpt,
} from "@concertable/shared/features/dashboard";
import type { OpportunityCard } from "@concertable/b2b/features/dashboard";
import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Application,
  ArtistDashboardKpis,
  ArtistDashboardOverview,
} from "./types";

const dashboardApi = {
  getOverview: async (): Promise<ArtistDashboardOverview> => {
    const { data } = await apiClient.get<ArtistDashboardOverview>(
      "/artistDashboard/overview",
    );
    return data;
  },
  getKpis: async (): Promise<ArtistDashboardKpis> => {
    const { data } = await apiClient.get<ArtistDashboardKpis>(
      "/artistDashboard/kpis",
    );
    return data;
  },
  getApplications: async (): Promise<Application[]> => {
    const { data } = await apiClient.get<Application[]>(
      "/application/artist/current",
    );
    return data;
  },
  getInbox: async (): Promise<MessagePreview[]> => {
    const { data } = await apiClient.get<MessagePreview[]>("/message/previews");
    return data;
  },
  getUpcomingConcerts: async (): Promise<ConcertCard[]> => {
    const { data } = await apiClient.get<ConcertCard[]>(
      "/concert/upcoming/artist/current",
    );
    return data;
  },
  getPayouts: async (): Promise<MonthlyRevenuePoint[]> => {
    const { data } = await apiClient.get<MonthlyRevenuePoint[]>(
      "/artistDashboard/charts/payouts",
    );
    return data;
  },
  getRecommendedOpportunities: async (): Promise<OpportunityCard[]> => {
    const { data } = await apiClient.get<OpportunityCard[]>(
      "/opportunity/artist/recommended",
    );
    return data;
  },
  getActivity: async (): Promise<ActivityItem[]> => {
    const { data } = await apiClient.get<ActivityItem[]>(
      "/artistDashboard/activity?take=10",
    );
    return data;
  },
  getRecentReviews: async (): Promise<ReviewExcerpt[]> => {
    const { data } = await apiClient.get<ReviewExcerpt[]>(
      "/artists/current/reviews/recent?take=5",
    );
    return data;
  },
};

export default dashboardApi;
