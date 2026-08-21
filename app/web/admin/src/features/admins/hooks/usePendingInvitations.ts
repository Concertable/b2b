import { toast } from "sonner";
import { useAdminOverviewQuery } from "./useAdminOverviewQuery";
import { useRevokeInvitationMutation } from "./useRevokeInvitationMutation";

export function usePendingInvitations() {
  const { data, isLoading } = useAdminOverviewQuery();
  const { mutate } = useRevokeInvitationMutation();

  const revoke = (id: string) =>
    mutate(id, { onSuccess: () => toast.success("Invitation revoked") });

  return { invitations: data?.pendingInvitations, isLoading, revoke };
}
