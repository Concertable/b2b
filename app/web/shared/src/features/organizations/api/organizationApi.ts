import { apiClient } from "@concertable/shared/lib/apiClient";
import type { Organization, UpdateOrganizationRequest } from "../types";

const BASE = "/organization";

const organizationApi = {
  get: async (): Promise<Organization | null> => {
    const { data, status } = await apiClient.get<Organization>(BASE);
    // 204 = no organization row yet. Return null, not undefined — a query function that
    // resolves to undefined throws in TanStack Query v5.
    return status === 204 ? null : data;
  },

  update: async (body: UpdateOrganizationRequest): Promise<Organization> => {
    const { data } = await apiClient.put<Organization>(BASE, body);
    return data;
  },
};

export default organizationApi;
