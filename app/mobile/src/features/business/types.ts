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
