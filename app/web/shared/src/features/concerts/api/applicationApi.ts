import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Checkout,
  ESignatureRequest,
} from "@concertable/shared/features/concerts/types";
import type { Application } from "../types";

const applicationApi = {
  applyToOpportunity: async (
    opportunityId: number,
    eSignature: ESignatureRequest,
  ): Promise<Application> => {
    const { data } = await apiClient.post<Application>(
      `/application/${opportunityId}`,
      { eSignature },
    );
    return data;
  },

  applyCheckout: async (opportunityId: number): Promise<Checkout> => {
    const { data } = await apiClient.post<Checkout>(
      `/application/opportunity/${opportunityId}/checkout`,
    );
    return data;
  },

  canApply: async (opportunityId: number): Promise<boolean> => {
    const { data } = await apiClient.get<boolean>(
      `/application/opportunity/${opportunityId}/eligibility`,
    );
    return data;
  },

  getApplicationsByOpportunityId: async (
    opportunityId: number,
  ): Promise<Application[]> => {
    const { data } = await apiClient.get<Application[]>(
      `/application/opportunity/${opportunityId}`,
    );
    return data;
  },

  getApplicationById: async (applicationId: number): Promise<Application> => {
    const { data } = await apiClient.get<Application>(
      `/application/${applicationId}`,
    );
    return data;
  },

  acceptApplication: async (
    applicationId: number,
    eSignature: ESignatureRequest,
  ): Promise<void> => {
    await apiClient.post(`/application/${applicationId}/accept`, { eSignature });
  },

  canAccept: async (applicationId: number): Promise<boolean> => {
    const { data } = await apiClient.get<boolean>(
      `/application/${applicationId}/eligibility`,
    );
    return data;
  },

  acceptCheckout: async (applicationId: number): Promise<Checkout> => {
    const { data } = await apiClient.post<Checkout>(
      `/application/${applicationId}/checkout`,
    );
    return data;
  },

  withdrawApplication: async (applicationId: number): Promise<void> => {
    await apiClient.post(`/application/${applicationId}/withdraw`);
  },

  rejectApplication: async (applicationId: number): Promise<void> => {
    await apiClient.post(`/application/${applicationId}/reject`);
  },

  cancelApplication: async (applicationId: number): Promise<void> => {
    await apiClient.post(`/application/${applicationId}/cancel`);
  },

  getPendingForArtist: async (): Promise<Application[]> => {
    const { data } = await apiClient.get<Application[]>(
      `/application/artist/pending`,
    );
    return data;
  },

  getRecentDeniedForArtist: async (): Promise<Application[]> => {
    const { data } = await apiClient.get<Application[]>(
      `/application/artist/recently-denied`,
    );
    return data;
  },
};

export default applicationApi;
