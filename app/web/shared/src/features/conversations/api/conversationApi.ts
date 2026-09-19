import { apiClient } from "@concertable/shared/lib/apiClient";
import type { MessagePreview } from "../types";

const conversationApi = {
  getPreviews: async (): Promise<MessagePreview[]> => {
    const { data } = await apiClient.get<MessagePreview[]>(
      "/conversations/previews",
    );
    return data;
  },
};

export default conversationApi;
