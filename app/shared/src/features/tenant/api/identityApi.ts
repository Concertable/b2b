import { apiClient } from "@concertable/shared/lib/apiClient";
import type { B2bIdentity } from "../types";

const identityApi = {
  getMe: async (): Promise<B2bIdentity> => {
    const { data } = await apiClient.get<B2bIdentity>("/auth/me");
    return data;
  },
};

export default identityApi;
