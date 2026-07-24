import { toast } from "sonner";
import {
  inviteMemberRequestSchema,
  type InviteMemberRequest,
} from "../schemas/inviteMemberRequestSchema";
import { useInviteMutation } from "./useInviteMutation";

export interface InviteBuffer {
  email: string;
  role: InviteMemberRequest["role"];
}

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
    roleOptions: inviteMemberRequestSchema.shape.role.options,
  };
}
