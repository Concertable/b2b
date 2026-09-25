import { useMutation, useQueryClient } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import type { ESignatureRequest } from "@concertable/shared/features/concerts/types";
import selfBillingAgreementApi from "../api/selfBillingAgreementApi";

export function useGrantSelfBillingAgreementMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (eSignature: ESignatureRequest) =>
      selfBillingAgreementApi.grant(eSignature),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: currentPrivateQueryKey("self-billing-agreement"),
      }),
  });
}
