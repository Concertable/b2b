import { toast } from "sonner";
import { useAdminOverviewQuery } from "./useAdminOverviewQuery";
import { useRevokeAdminMutation } from "./useRevokeAdminMutation";

export function useAdminsRoster() {
  const { data, isLoading } = useAdminOverviewQuery();
  const { mutate } = useRevokeAdminMutation();

  const revoke = (sub: string) =>
    mutate(sub, { onSuccess: () => toast.success("Admin revoked") });

  return {
    admins: data?.admins,
    isLoading,
    canRevoke: (data?.admins.length ?? 0) > 1,
    revoke,
  };
}
