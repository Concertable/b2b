import { toast } from "sonner";
import { useVerificationQuery } from "./useVerificationQuery";
import { useSubmitVerificationMutation } from "./useSubmitVerificationMutation";
import { submitVerificationRequestSchema } from "../schemas/submitVerificationRequestSchema";
import type { VerificationDocumentType } from "../types";

export type EvidenceBuffer = Partial<Record<VerificationDocumentType, File>>;

export function useVerification() {
  const { data: verification, isLoading } = useVerificationQuery();
  const { mutate, isPending } = useSubmitVerificationMutation();

  const submit = (buffer: EvidenceBuffer, onDone?: () => void) => {
    const documents = (
      Object.entries(buffer) as [VerificationDocumentType, File | undefined][]
    )
      .filter(([, file]) => file != null)
      .map(([documentType, file]) => ({ documentType, file }));

    const parsed = submitVerificationRequestSchema.safeParse({ documents });
    if (parsed.success) {
      mutate(parsed.data, {
        onSuccess: () => {
          toast.success("Verification evidence submitted");
          onDone?.();
        },
      });
    }
    return parsed;
  };

  return { verification, isLoading, submit, isSubmitting: isPending };
}
