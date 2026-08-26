import type {
  ActivityItem,
  ConcertCard,
  MonthlyRevenuePoint,
  ReviewExcerpt,
  Settlement,
} from "@concertable/shared/features/dashboard/types";
import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Application,
  OpportunityApplicationMetrics,
  VenueDashboardKpis,
  VenueDashboardOverview,
} from "./types";

const dashboardApi = {
  getOverview: async (): Promise<VenueDashboardOverview> => {
    const { data } = await apiClient.get<VenueDashboardOverview>(
      "/venue-dashboard/overview",
    );
    return data;
  },
  getKpis: async (): Promise<VenueDashboardKpis> => {
    const { data } = await apiClient.get<VenueDashboardKpis>(
      "/venue-dashboard/kpis",
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
      "/venue-dashboard/charts/ticket-revenue",
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
      "/venue-dashboard/activity",
    );
    return data;
  },
  getSettlements: async (): Promise<Settlement[]> => {
    const { data } = await apiClient.get<Settlement[]>(
      "/venue-dashboard/settlements",
    );
    return data;
  },
  getRecentReviews: async (): Promise<ReviewExcerpt[]> => {
    const { data } = await apiClient.get<ReviewExcerpt[]>(
      "/organization/venue/review/recent",
    );
    return data;
  },
};

export default dashboardApi;
