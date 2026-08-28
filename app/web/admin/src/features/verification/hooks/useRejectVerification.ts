import { toast } from "sonner";
import { useRejectVerificationMutation } from "./useRejectVerificationMutation";
import type { RejectVerificationRequest } from "../schemas/rejectVerificationRequestSchema";

export function useRejectVerification(tenantId: string) {
  const { mutate, isPending } = useRejectVerificationMutation();

  const submit = (request: RejectVerificationRequest, onDone: () => void) => {
    mutate(
      { tenantId, request },
      {
        onSuccess: () => {
          toast.success("Verification rejected");
          onDone();
        },
      },
    );
  };

  return { submit, isPending };
}
