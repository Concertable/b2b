import type { ActionLink } from "@concertable/shared/types/common";
import type { z } from "zod";
import type { reportMessageRequestSchema } from "./schemas/reportMessageRequestSchema";

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

export interface ConversationMessage {
  id: number;
  conversationId: number;
  sequence: number;
  senderTenantId: string;
  sentByUserId: string;
  content: string;
  sentAt: string;
  actions: { report?: ActionLink | null };
}

export interface AdvanceConversationReadPositionRequest {
  throughSequence: number;
}

export interface ConversationReadPosition extends AdvanceConversationReadPositionRequest {
  conversationId: number;
}

export interface SelectedMessage {
  conversationId: number;
  messageId: number;
}

export type ReportMessageRequest = z.output<typeof reportMessageRequestSchema>;
export type ReportMessageFormValues = z.input<typeof reportMessageRequestSchema>;
