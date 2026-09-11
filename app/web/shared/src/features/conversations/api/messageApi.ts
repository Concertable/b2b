import { apiClient } from "@concertable/shared/lib/apiClient";
import type { MessagePreview } from "../types";

const messageApi = {
  getPreviews: async (): Promise<MessagePreview[]> => {
    const { data } = await apiClient.get<MessagePreview[]>("/message/previews");
    return data;
  },
};

export default messageApi;
