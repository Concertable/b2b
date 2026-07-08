import { useMutation } from "@tanstack/react-query";
import applicationApi from "@concertable/shared/features/concerts/api/applicationApi";

// Web-only: the object-URL + anchor download uses the DOM, so it can't live in the
// cross-platform @concertable/shared core. Both manager apps gate this on actions.agreement.
export function useDownloadAgreement() {
  return useMutation({
    mutationFn: async (applicationId: number) => {
      const blob = await applicationApi.getAgreementPdf(applicationId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `booking-agreement-${applicationId}.pdf`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    },
  });
}
