import { apiClient } from "@concertable/shared/lib/apiClient";
import type { Member, Invitation, ChangeMemberRoleRequest } from "../types";
import type { InviteMemberRequest } from "../schemas/inviteMemberRequestSchema";

const membersApi = {
  listMembers: async (): Promise<Member[]> => {
    const { data } = await apiClient.get<Member[]>("/organizations/members");
    return data;
  },

  listInvitations: async (): Promise<Invitation[]> => {
    const { data } = await apiClient.get<Invitation[]>("/organizations/invitations");
    return data;
  },

  invite: async (body: InviteMemberRequest): Promise<Invitation> => {
    const { data } = await apiClient.post<Invitation>(
      "/organizations/invitations",
      body,
    );
    return data;
  },

  revokeInvitation: async (id: string): Promise<void> => {
    await apiClient.delete(`/organizations/invitations/${id}`);
  },

  changeRole: async (
    userId: string,
    body: ChangeMemberRoleRequest,
  ): Promise<void> => {
    await apiClient.put(`/organizations/members/${userId}/role`, body);
  },

  removeMember: async (userId: string): Promise<void> => {
    await apiClient.delete(`/organizations/members/${userId}`);
  },
};

export default membersApi;
