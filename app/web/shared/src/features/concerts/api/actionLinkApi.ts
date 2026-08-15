import { apiClient } from "@concertable/shared/lib/apiClient";
import type { ActionLink } from "@concertable/shared/types/common";

function apiPath(href: string) {
  return href.replace(/^\/api(?=\/)/i, "");
}

const actionLinkApi = {
  execute: async (action: ActionLink): Promise<void> => {
    await apiClient.request({
      url: apiPath(action.href),
      method: action.method,
    });
  },

  download: async (action: ActionLink, fileName: string): Promise<void> => {
    const { data } = await apiClient.request<Blob>({
      url: apiPath(action.href),
      method: action.method,
      responseType: "blob",
    });
    const url = URL.createObjectURL(data);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  },
};

export default actionLinkApi;
