import { toast } from "sonner";
import { useInvitationsQuery } from "./useInvitationsQuery";
import { useRevokeInvitationMutation } from "./useRevokeInvitationMutation";

export function usePendingInvitations() {
  const { data: invitations, isLoading } = useInvitationsQuery();
  const { mutate } = useRevokeInvitationMutation();

  const revoke = (id: string) =>
    mutate(id, { onSuccess: () => toast.success("Invitation revoked") });

  return { invitations, isLoading, revoke };
}
