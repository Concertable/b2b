import { useMutation, useQueryClient } from "@tanstack/react-query";
import moderationApi from "../api/moderationApi";
import { moderationKeys } from "./moderationKeys";

export function useRestoreMessageMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: moderationApi.restoreMessage,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: moderationKeys.reports }),
  });
}
