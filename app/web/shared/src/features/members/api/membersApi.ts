import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Member,
  Invitation,
  ChangeMemberRoleRequest,
  InviteMemberRequest,
} from "../types";

const membersApi = {
  listMembers: async (): Promise<Member[]> => {
    const { data } = await apiClient.get<Member[]>("/organization/members");
    return data;
  },

  listInvitations: async (): Promise<Invitation[]> => {
    const { data } = await apiClient.get<Invitation[]>("/organization/invitations");
    return data;
  },

  invite: async (body: InviteMemberRequest): Promise<Invitation> => {
    const { data } = await apiClient.post<Invitation>(
      "/organization/invitations",
      body,
    );
    return data;
  },

  revokeInvitation: async (id: string): Promise<void> => {
    await apiClient.delete(`/organization/invitations/${id}`);
  },

  changeRole: async (
    userId: string,
    body: ChangeMemberRoleRequest,
  ): Promise<void> => {
    await apiClient.put(`/organization/members/${userId}/role`, body);
  },

  removeMember: async (userId: string): Promise<void> => {
    await apiClient.delete(`/organization/members/${userId}`);
  },
};

export default membersApi;
