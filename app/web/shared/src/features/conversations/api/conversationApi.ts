import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  AdvanceConversationReadPositionRequest,
  ConversationMessage,
  MessagePreview,
  ReportMessageRequest,
} from "../types";

const conversationApi = {
  getPreviews: async (
    pageNumber = 1,
    signal?: AbortSignal,
  ): Promise<MessagePreview[]> => {
    const { data } = await apiClient.get<MessagePreview[]>(
      "/conversations/previews",
      { params: { pageNumber }, signal },
    );
    return data;
  },

  getUnreadCount: async (signal?: AbortSignal): Promise<number> => {
    const { data } = await apiClient.get<number>(
      "/conversations/unread-count",
      { signal },
    );
    return data;
  },

  getMessages: async (
    conversationId: number,
    signal?: AbortSignal,
  ): Promise<ConversationMessage[]> => {
    const { data } = await apiClient.get<ConversationMessage[]>(
      `/conversations/${conversationId}/messages`,
      { signal },
    );
    return data;
  },

  advanceReadPosition: async (
    conversationId: number,
    request: AdvanceConversationReadPositionRequest,
  ): Promise<void> => {
    await apiClient.put(
      `/conversations/${conversationId}/read-position`,
      request,
    );
  },

  reportMessage: async (
    conversationId: number,
    messageId: number,
    request: ReportMessageRequest,
  ): Promise<void> => {
    await apiClient.post(
      `/conversations/${conversationId}/messages/${messageId}/report`,
      request,
    );
  },
};

export default conversationApi;
