import { toast } from "sonner";
import { inviteAdminRequestSchema } from "../schemas/inviteAdminRequestSchema";
import { useInviteMutation } from "./useInviteMutation";

export interface InviteDraft {
  email: string;
}

export function useInviteAdmin() {
  const { mutate, isPending } = useInviteMutation();

  const validate = (draft: InviteDraft) =>
    inviteAdminRequestSchema.safeParse(draft);

  const submit = (draft: InviteDraft, onDone: () => void) => {
    const parsed = validate(draft);
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
