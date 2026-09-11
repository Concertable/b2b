import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { ESignatureRequest } from "@concertable/shared/features/concerts/types";
import selfBillingAgreementApi from "../api/selfBillingAgreementApi";

export function useGrantSelfBillingAgreementMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (eSignature: ESignatureRequest) =>
      selfBillingAgreementApi.grant(eSignature),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["self-billing-agreement"] }),
  });
}
