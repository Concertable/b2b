import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Checkout,
  ESignatureRequest,
} from "@concertable/shared/features/concerts/types";
import type { ApplicationProposal } from "../types";

const applicationApi = {
  applyToOpportunity: async (
    opportunityId: number,
    eSignature: ESignatureRequest,
  ): Promise<ApplicationProposal> => {
    const { data } = await apiClient.post<ApplicationProposal>(
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
  ): Promise<ApplicationProposal[]> => {
    const { data } = await apiClient.get<ApplicationProposal[]>(
      `/application/opportunity/${opportunityId}`,
    );
    return data;
  },

  getProposal: async (applicationId: number): Promise<ApplicationProposal> => {
    const { data } = await apiClient.get<ApplicationProposal>(
      `/application/${applicationId}/proposal`,
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

  getPendingForArtist: async (): Promise<ApplicationProposal[]> => {
    const { data } = await apiClient.get<ApplicationProposal[]>(
      `/application/artist/pending`,
    );
    return data;
  },

  getRecentDeniedForArtist: async (): Promise<ApplicationProposal[]> => {
    const { data } = await apiClient.get<ApplicationProposal[]>(
      `/application/artist/recently-denied`,
    );
    return data;
  },
};

export default applicationApi;
