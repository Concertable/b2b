import { apiClient } from "@concertable/web/lib/apiClient";
import type { AdminInvitation, AdminOverview } from "../types";
import type { InviteAdminRequest } from "../schemas/inviteAdminRequestSchema";

const adminApi = {
  getOverview: async (): Promise<AdminOverview> => {
    const { data } = await apiClient.get<AdminOverview>("/Admin");
    return data;
  },

  revokeAdmin: async (sub: string): Promise<void> => {
    await apiClient.delete(`/Admin/${sub}`);
  },

  invite: async (body: InviteAdminRequest): Promise<AdminInvitation> => {
    const { data } = await apiClient.post<AdminInvitation>(
      "/AdminInvitation",
      body,
    );
    return data;
  },

  revokeInvitation: async (id: string): Promise<void> => {
    await apiClient.delete(`/AdminInvitation/${id}`);
  },
};

export default adminApi;
