import { useMutation, useQueryClient } from "@tanstack/react-query";
import verificationApi from "../api/verificationApi";
import { verificationKeys } from "./verificationKeys";

export function useApproveVerificationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: verificationApi.approve,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: verificationKeys.pending }),
  });
}
