import { useMutation } from "@tanstack/react-query";
import { apiClient } from "@concertable/shared/lib/apiClient";

export function useDownloadContractMutation() {
  return useMutation({
    mutationFn: async (applicationId: number) => {
      const { data } = await apiClient.get<ArrayBuffer>(
        `/application/${applicationId}/contract/pdf`,
        { responseType: "arraybuffer" },
      );
      const blob = new Blob([data], { type: "application/pdf" });
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `contract-${applicationId}.pdf`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    },
  });
}
