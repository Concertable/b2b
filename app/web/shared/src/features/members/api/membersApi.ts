import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Member,
  Invitation,
  ChangeMemberRoleRequest,
  InviteMemberRequest,
} from "../types";

const BASE = "/organization";

const membersApi = {
  listMembers: async (): Promise<Member[]> => {
    const { data } = await apiClient.get<Member[]>(`${BASE}/members`);
    return data;
  },

  listInvitations: async (): Promise<Invitation[]> => {
    const { data } = await apiClient.get<Invitation[]>(`${BASE}/invitations`);
    return data;
  },

  invite: async (body: InviteMemberRequest): Promise<Invitation> => {
    const { data } = await apiClient.post<Invitation>(
      `${BASE}/invitations`,
      body,
    );
    return data;
  },

  revokeInvitation: async (id: string): Promise<void> => {
    await apiClient.delete(`${BASE}/invitations/${id}`);
  },

  changeRole: async (
    userId: string,
    body: ChangeMemberRoleRequest,
  ): Promise<void> => {
    await apiClient.put(`${BASE}/members/${userId}/role`, body);
  },

  removeMember: async (userId: string): Promise<void> => {
    await apiClient.delete(`${BASE}/members/${userId}`);
  },
};

export default membersApi;
