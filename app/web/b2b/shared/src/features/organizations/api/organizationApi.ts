import { apiClient } from "@concertable/shared/lib/apiClient";
import type { Organization, UpdateOrganizationRequest } from "../types";

const BASE = "/organization";

const organizationApi = {
  get: async (): Promise<Organization | undefined> => {
    const { data, status } = await apiClient.get<Organization>(BASE);
    return status === 204 ? undefined : data;
  },

  update: async (body: UpdateOrganizationRequest): Promise<Organization> => {
    const { data } = await apiClient.put<Organization>(BASE, body);
    return data;
  },
};

export default organizationApi;
