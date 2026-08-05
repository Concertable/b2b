import { apiClient } from "@concertable/shared/lib/apiClient";
import type { ESignatureRequest } from "@concertable/shared/features/concerts/types";
import type { SelfBillingAgreement } from "../types";

const path = "/self-billing-agreement";

const selfBillingAgreementApi = {
  get: async (): Promise<SelfBillingAgreement> => {
    const { data } = await apiClient.get<SelfBillingAgreement>(path);
    return data;
  },

  grant: async (eSignature: ESignatureRequest): Promise<void> => {
    await apiClient.post(path, { eSignature });
  },

  getPdf: async (): Promise<Blob> => {
    const { data } = await apiClient.get<ArrayBuffer>(`${path}/pdf`, {
      responseType: "arraybuffer",
    });
    return new Blob([data], { type: "application/pdf" });
  },
};

export default selfBillingAgreementApi;
