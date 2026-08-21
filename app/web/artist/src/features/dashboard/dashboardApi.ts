import type {
  ActivityItem,
  ConcertCard,
  MonthlyRevenuePoint,
  ReviewExcerpt,
} from "@concertable/shared/features/dashboard";
import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Application,
  ArtistDashboardKpis,
  ArtistDashboardOverview,
  OpportunityMatch,
} from "./types";

const dashboardApi = {
  getOverview: async (): Promise<ArtistDashboardOverview> => {
    const { data } = await apiClient.get<ArtistDashboardOverview>(
      "/artist-dashboard/overview",
    );
    return data;
  },
  getKpis: async (): Promise<ArtistDashboardKpis> => {
    const { data } = await apiClient.get<ArtistDashboardKpis>(
      "/artist-dashboard/kpis",
    );
    return data;
  },
  getApplications: async (): Promise<Application[]> => {
    const { data } = await apiClient.get<Application[]>(
      "/application/artist/current",
    );
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
      "/artist-dashboard/charts/payouts",
    );
    return data;
  },
  getRecommendedOpportunities: async (): Promise<OpportunityMatch[]> => {
    const { data } = await apiClient.get<OpportunityMatch[]>(
      "/opportunity/artist/recommended",
    );
    return data;
  },
  getActivity: async (): Promise<ActivityItem[]> => {
    const { data } = await apiClient.get<ActivityItem[]>(
      "/artist-dashboard/activity",
    );
    return data;
  },
  getRecentReviews: async (): Promise<ReviewExcerpt[]> => {
    const { data } = await apiClient.get<ReviewExcerpt[]>(
      "/organization/artist/review/recent",
    );
    return data;
  },
};

export default dashboardApi;
