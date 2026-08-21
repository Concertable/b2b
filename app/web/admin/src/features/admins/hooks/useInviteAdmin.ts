import { toast } from "sonner";
import { inviteAdminRequestSchema } from "../schemas/inviteAdminRequestSchema";
import { useInviteMutation } from "./useInviteMutation";

export interface InviteBuffer {
  email: string;
}

export function useInviteAdmin() {
  const { mutate, isPending } = useInviteMutation();

  const validate = (buffer: InviteBuffer) =>
    inviteAdminRequestSchema.safeParse(buffer);

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

  return { submit, validate, isPending };
}
