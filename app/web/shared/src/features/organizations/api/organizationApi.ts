import { apiClient } from "@concertable/shared/lib/apiClient";
import type { Organization, UpdateOrganizationRequest } from "../types";

const organizationApi = {
  get: async (): Promise<Organization | undefined> => {
    const { data, status } = await apiClient.get<Organization>("/organization");
    return status === 204 ? undefined : data;
  },

  update: async (body: UpdateOrganizationRequest): Promise<Organization> => {
    const { data } = await apiClient.put<Organization>("/organization", body);
    return data;
  },
};

export default organizationApi;
