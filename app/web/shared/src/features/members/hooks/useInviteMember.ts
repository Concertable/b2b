import { toast } from "sonner";
import type { InviteMemberRequest } from "../types";
import { useInviteMutation } from "./useInviteMutation";
import { useRolesQuery } from "./useRolesQuery";

export function useInviteMember() {
  const { mutate, isPending } = useInviteMutation();
  const { data: roles, isLoading: rolesLoading } = useRolesQuery();
  const submit = (request: InviteMemberRequest, onDone: () => void) =>
    mutate(request, {
      onSuccess: () => {
        toast.success("Invitation sent");
        onDone();
      },
    });
  return { submit, isPending, roles: roles ?? [], rolesLoading };
}
