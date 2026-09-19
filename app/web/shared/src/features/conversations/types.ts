export interface ConversationParticipant {
  tenantId: string;
  displayName: string;
}

export interface MessagePreview {
  id: number;
  conversationId: number;
  participants: ConversationParticipant[];
  preview: string;
  at: string;
  unread: boolean;
  href: string;
}
