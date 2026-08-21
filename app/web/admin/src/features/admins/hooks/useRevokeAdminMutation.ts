import { useMutation, useQueryClient } from "@tanstack/react-query";
import adminApi from "../api/adminApi";
import { adminOverviewQueryKey } from "./useAdminOverviewQuery";

export function useRevokeAdminMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: adminApi.revokeAdmin,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: adminOverviewQueryKey }),
  });
}
