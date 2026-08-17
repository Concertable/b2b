import { apiClient } from "@concertable/web/lib/apiClient";
import type { Identity } from "../types";

const identityApi = {
  getMe: async (): Promise<Identity> => {
    const { data } = await apiClient.get<Identity>("/auth/me");
    return data;
  },
};

export default identityApi;
