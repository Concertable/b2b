import { toast } from "sonner";
import type { InviteMemberRequest } from "../types";
import { INVITE_MEMBER_ROLES } from "../types";
import { inviteMemberRequestSchema } from "../schemas/inviteMemberRequestSchema";
import { useInviteMutation } from "./useInviteMutation";

export type InviteBuffer = InviteMemberRequest;

export function useInviteMember() {
  const { mutate, isPending } = useInviteMutation();

  const validate = (buffer: InviteBuffer) =>
    inviteMemberRequestSchema.safeParse(buffer);

  const submit = (buffer: InviteBuffer, onDone: () => void) => {
    const parsed = validate(buffer);
    if (parsed.success)
      mutate(parsed.data, {
        onSuccess: () => {
          toast.success("Invitation sent");
          onDone();
        },
      });
    return parsed;
  };

  return {
    submit,
    validate,
    isPending,
    roleOptions: INVITE_MEMBER_ROLES,
  };
}
