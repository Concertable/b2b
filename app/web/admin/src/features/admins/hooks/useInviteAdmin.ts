import { toast } from "sonner";
import { useInviteMutation } from "./useInviteMutation";
import type { InviteAdminRequest } from "../types";

export function useInviteAdmin() {
  const { mutate, isPending } = useInviteMutation();

  const submit = (request: InviteAdminRequest, onDone: () => void) => {
    mutate(request, {
      onSuccess: () => {
        toast.success("Invitation sent");
        onDone();
      },
    });
  };

  return { submit, isPending };
}
