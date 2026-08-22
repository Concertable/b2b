import { useMutation, useQueryClient } from "@tanstack/react-query";
import venuesApi from "../api/venuesApi";
import { venuesKeys } from "./venuesKeys";

export function useApproveVenueMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: venuesApi.approve,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: venuesKeys.pendingApproval }),
  });
}
