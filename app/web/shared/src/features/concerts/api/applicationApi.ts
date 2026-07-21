import api from "@concertable/shared/lib/axiosClient";
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
    const { data } = await api.post<Application>(
      `/application/${opportunityId}`,
      { eSignature },
    );
    return data;
  },

  applyToOpportunityWithPayment: async (
    opportunityId: number,
    paymentMethodId: string,
    eSignature: ESignatureRequest,
  ): Promise<Application> => {
    const { data } = await api.post<Application>(
      `/application/${opportunityId}`,
      { eSignature, paymentMethodId },
    );
    return data;
  },

  applyCheckout: async (opportunityId: number): Promise<Checkout> => {
    const { data } = await api.post<Checkout>(
      `/application/opportunity/${opportunityId}/checkout`,
    );
    return data;
  },

  canApply: async (opportunityId: number): Promise<boolean> => {
    const { data } = await api.get<boolean>(
      `/application/opportunity/${opportunityId}/eligibility`,
    );
    return data;
  },

  getApplicationsByOpportunityId: async (
    opportunityId: number,
  ): Promise<Application[]> => {
    const { data } = await api.get<Application[]>(
      `/application/opportunity/${opportunityId}`,
    );
    return data;
  },

  getApplicationById: async (applicationId: number): Promise<Application> => {
    const { data } = await api.get<Application>(
      `/application/${applicationId}`,
    );
    return data;
  },

  acceptApplication: async (
    applicationId: number,
    eSignature: ESignatureRequest,
    body?: { paymentMethodId: string },
  ): Promise<void> => {
    await api.post(`/application/${applicationId}/accept`, {
      eSignature,
      ...body,
    });
  },

  canAccept: async (applicationId: number): Promise<boolean> => {
    const { data } = await api.get<boolean>(
      `/application/${applicationId}/eligibility`,
    );
    return data;
  },

  acceptCheckout: async (applicationId: number): Promise<Checkout> => {
    const { data } = await api.post<Checkout>(
      `/application/${applicationId}/checkout`,
    );
    return data;
  },

  withdrawApplication: async (applicationId: number): Promise<void> => {
    await api.post(`/application/${applicationId}/withdraw`);
  },

  rejectApplication: async (applicationId: number): Promise<void> => {
    await api.post(`/application/${applicationId}/reject`);
  },

  cancelApplication: async (applicationId: number): Promise<void> => {
    await api.post(`/application/${applicationId}/cancel`);
  },

  getPendingForArtist: async (): Promise<Application[]> => {
    const { data } = await api.get<Application[]>(
      `/application/artist/pending`,
    );
    return data;
  },

  getRecentDeniedForArtist: async (): Promise<Application[]> => {
    const { data } = await api.get<Application[]>(
      `/application/artist/recently-denied`,
    );
    return data;
  },
};

export default applicationApi;
