import { toast } from "sonner";
import type { InviteMemberRequest } from "../types";
import { INVITE_MEMBER_ROLES } from "../types";
import { useInviteMutation } from "./useInviteMutation";

export function useInviteMember() {
  const { mutate, isPending } = useInviteMutation();

  const submit = (request: InviteMemberRequest, onDone: () => void) =>
    mutate(request, {
      onSuccess: () => {
        toast.success("Invitation sent");
        onDone();
      },
    });

  return {
    submit,
    isPending,
    roleOptions: INVITE_MEMBER_ROLES,
  };
}
