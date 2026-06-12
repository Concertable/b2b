import api from "@concertable/shared/lib/axiosClient";
import type { Organization, UpdateOrganizationRequest } from "../types";

const organizationApi = {
  get: async (): Promise<Organization | null> => {
    const { data, status } = await api.get<Organization>("/organizations");
    return status === 204 ? null : data;
  },

  update: async (body: UpdateOrganizationRequest): Promise<Organization> => {
    const { data } = await api.put<Organization>("/organizations", body);
    return data;
  },
};

export default organizationApi;
