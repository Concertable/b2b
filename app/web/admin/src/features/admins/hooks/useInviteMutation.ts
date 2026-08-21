import { useMutation, useQueryClient } from "@tanstack/react-query";
import adminApi from "../api/adminApi";
import { adminOverviewQueryKey } from "./useAdminOverviewQuery";

export function useInviteMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.invite,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: adminOverviewQueryKey }),
  });
}
