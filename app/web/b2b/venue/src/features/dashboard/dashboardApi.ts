import type {
  ActivityItem,
  ConcertCard,
  MonthlyRevenuePoint,
  ReviewExcerpt,
  Settlement,
} from "@concertable/shared/features/dashboard";
import type { OpportunityApplicationMetrics } from "@concertable/b2b/features/dashboard";
import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Application,
  VenueDashboardKpis,
  VenueDashboardOverview,
} from "./types";

const dashboardApi = {
  getOverview: async (): Promise<VenueDashboardOverview> => {
    const { data } = await apiClient.get<VenueDashboardOverview>(
      "/venueDashboard/overview",
    );
    return data;
  },
  getKpis: async (): Promise<VenueDashboardKpis> => {
    const { data } = await apiClient.get<VenueDashboardKpis>(
      "/venueDashboard/kpis",
    );
    return data;
  },
  getApplicationsToReview: async (): Promise<Application[]> => {
    const { data } = await apiClient.get<Application[]>(
      "/application/venue/current",
    );
    return data;
  },
  getUpcomingConcerts: async (): Promise<ConcertCard[]> => {
    const { data } = await apiClient.get<ConcertCard[]>(
      "/concert/upcoming/venue/current",
    );
    return data;
  },
  getTicketRevenue: async (): Promise<MonthlyRevenuePoint[]> => {
    const { data } = await apiClient.get<MonthlyRevenuePoint[]>(
      "/venueDashboard/charts/ticket-revenue",
    );
    return data;
  },
  getOpenOpportunities: async (): Promise<OpportunityApplicationMetrics[]> => {
    const { data } = await apiClient.get<OpportunityApplicationMetrics[]>(
      "/opportunity/venue/current",
    );
    return data;
  },
  getActivity: async (): Promise<ActivityItem[]> => {
    const { data } = await apiClient.get<ActivityItem[]>(
      "/venueDashboard/activity",
    );
    return data;
  },
  getSettlements: async (): Promise<Settlement[]> => {
    const { data } = await apiClient.get<Settlement[]>(
      "/venueDashboard/settlements",
    );
    return data;
  },
  getRecentReviews: async (): Promise<ReviewExcerpt[]> => {
    const { data } = await apiClient.get<ReviewExcerpt[]>(
      "/venues/current/reviews/recent",
    );
    return data;
  },
};

export default dashboardApi;
