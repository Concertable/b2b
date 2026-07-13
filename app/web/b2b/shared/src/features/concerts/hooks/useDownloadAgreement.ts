import { useMutation } from "@tanstack/react-query";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";

// Web-only: the object-URL + anchor download uses the DOM, so it can't live in the
// cross-platform @concertable/shared core. Gated on the concert's actions.agreement link.
export function useDownloadAgreement() {
  return useMutation({
    mutationFn: async (concertId: number) => {
      const blob = await concertApi.getAgreementPdf(concertId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = `booking-agreement-${concertId}.pdf`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    },
  });
}
