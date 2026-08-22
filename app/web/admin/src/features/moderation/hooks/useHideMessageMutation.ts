import { useMutation, useQueryClient } from "@tanstack/react-query";
import moderationApi from "../api/moderationApi";
import { moderationKeys } from "./moderationKeys";

export function useHideMessageMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: moderationApi.hideMessage,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: moderationKeys.reports }),
  });
}
