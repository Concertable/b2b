import { useMutation, useQueryClient } from "@tanstack/react-query";
import verificationApi from "../api/verificationApi";
import { verificationKeys } from "./verificationKeys";
import type { RejectVerificationRequest } from "../schemas/rejectVerificationRequestSchema";

interface RejectVerificationVariables {
  tenantId: string;
  request: RejectVerificationRequest;
}

export function useRejectVerificationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ tenantId, request }: RejectVerificationVariables) =>
      verificationApi.reject(tenantId, request),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: verificationKeys.pending }),
  });
}
