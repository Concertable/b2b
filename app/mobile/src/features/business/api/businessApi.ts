import { apiClient } from "@concertable/mobile/lib/apiClient";

import type {
  OrganizationDetails,
  OrganizationMember,
  ConversationPreview,
  ConcertSummary,
} from "../types";

export const businessApi = {
  getOrganization: async () =>
    (await apiClient.get<OrganizationDetails>("/organization")).data,
  getMembers: async () =>
    (await apiClient.get<OrganizationMember[]>("/organization/members")).data,
  getConversations: async () =>
    (await apiClient.get<ConversationPreview[]>("/conversations/previews"))
      .data,
  getConcertSummary: async (id: number) =>
    (await apiClient.get<ConcertSummary>(`/concert/${id}/summary`)).data,
};
