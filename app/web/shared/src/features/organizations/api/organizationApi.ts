import { apiClient } from "@concertable/shared/lib/apiClient";
import type { Organization } from "../types";
import type { UpdateOrganizationRequest } from "../schemas/updateOrganizationRequestSchema";

const organizationApi = {
  get: async (): Promise<Organization | null> => {
    const { data, status } = await apiClient.get<Organization>("/organization");
    return status === 204 ? null : data;
  },

  update: async (body: UpdateOrganizationRequest): Promise<Organization> => {
    const { data } = await apiClient.put<Organization>("/organization", body);
    return data;
  },
};

export default organizationApi;
