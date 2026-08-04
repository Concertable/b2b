import { useMutation } from "@tanstack/react-query";
import selfBillingAgreementApi from "../api/selfBillingAgreementApi";

// Web-only: the object-URL + anchor download uses the DOM, so it can't live in the
// cross-platform @concertable/shared core. Gated on the agreement's actions.pdf link.
export function useDownloadSelfBillingAgreementMutation() {
  return useMutation({
    mutationFn: async () => {
      const blob = await selfBillingAgreementApi.getPdf();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = "self-billing-agreement.pdf";
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    },
  });
}
