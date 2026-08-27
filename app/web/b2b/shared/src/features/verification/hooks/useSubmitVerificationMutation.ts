import { useMutation, useQueryClient } from "@tanstack/react-query";
import verificationApi from "../api/verificationApi";
import { verificationKeys } from "./verificationKeys";
import type { SubmitVerificationRequest } from "../schemas/submitVerificationRequestSchema";

export function useSubmitVerificationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: SubmitVerificationRequest) =>
      verificationApi.submitDocuments(request),
    onSuccess: (verification) =>
      queryClient.setQueryData(verificationKeys.status, verification),
  });
}
