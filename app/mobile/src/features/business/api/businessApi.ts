import { apiClient } from "@concertable/mobile/lib/apiClient";

export interface OrganizationDetails {
  readonly id: string;
  readonly legalName: string;
  readonly contactEmail: string;
  readonly version: number;
  readonly eligibilityVersion: number;
  readonly businessActivities: ReadonlyArray<string>;
}

export interface OrganizationMember {
  readonly userId: string;
  readonly email: string;
  readonly role: string;
}

export interface ConversationPreview {
  readonly id: number;
  readonly conversationId: number;
  readonly preview: string;
  readonly at: string;
  readonly unread: boolean;
  readonly participants: ReadonlyArray<{
    readonly tenantId: string;
    readonly displayName: string;
  }>;
}

export interface ConcertSummary {
  readonly id: number;
  readonly name: string;
  readonly startDate: string;
  readonly endDate: string;
  readonly venueName: string;
  readonly artistName: string;
  readonly state: string;
}

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
