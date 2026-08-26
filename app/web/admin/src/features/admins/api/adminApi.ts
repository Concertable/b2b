import { apiClient } from "@concertable/web/lib/apiClient";
import type { AdminInvitation, AdminOverview } from "../types";
import type { InviteAdminRequest } from "../schemas/inviteAdminRequestSchema";

const BASE = "/Admin";
const INVITATION_BASE = "/AdminInvitation";

const adminApi = {
  getOverview: async (): Promise<AdminOverview> => {
    const { data } = await apiClient.get<AdminOverview>(BASE);
    return data;
  },

  revokeAdmin: async (sub: string): Promise<void> => {
    await apiClient.delete(`${BASE}/${sub}`);
  },

  invite: async (body: InviteAdminRequest): Promise<AdminInvitation> => {
    const { data } = await apiClient.post<AdminInvitation>(
      INVITATION_BASE,
      body,
    );
    return data;
  },

  revokeInvitation: async (id: string): Promise<void> => {
    await apiClient.delete(`${INVITATION_BASE}/${id}`);
  },
};

export default adminApi;
