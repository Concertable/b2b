import { useMutation, useQuery } from "@tanstack/react-query";
import { useAuthStore } from "@/features/auth";
import applicationApi from "@b2b/features/concerts/api/applicationApi";
import type { ESignatureRequest } from "@concertable/shared/features/concerts/types";

export function useApply(opportunityId: number, options?: { onSuccess?: () => void }) {
  const isAuthenticated = useAuthStore((s) => s.user != null);

  const { data: isEligible } = useQuery({
    queryKey: ["applications", "opportunity", opportunityId, "eligibility"],
    queryFn: () => applicationApi.canApply(opportunityId),
    enabled: isAuthenticated,
  });

  const canApply = isEligible === true;

  const {
    mutate: applyMutate,
    isPending,
    error,
  } = useMutation({
    mutationFn: (eSignature: ESignatureRequest) =>
      applicationApi.applyToOpportunity(opportunityId, eSignature),
    onSuccess: () => options?.onSuccess?.(),
  });

  const apply = (eSignature: ESignatureRequest) => applyMutate(eSignature);

  return { apply, isPending, error, canApply };
}
