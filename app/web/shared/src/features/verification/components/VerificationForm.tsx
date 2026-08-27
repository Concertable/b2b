import { useState } from "react";
import { Button } from "@concertable/web/components/ui/button";
import { Input } from "@concertable/web/components/ui/input";
import { Label } from "@concertable/web/components/ui/label";
import { useVerification, type EvidenceBuffer } from "../hooks/useVerification";
import {
  VERIFICATION_DOCUMENT_TYPE_LABELS,
  type VerificationDocumentType,
} from "../types";

const DOCUMENT_TYPES = Object.keys(
  VERIFICATION_DOCUMENT_TYPE_LABELS,
) as VerificationDocumentType[];

const ACCEPT = "application/pdf,image/jpeg,image/png";

interface FieldErrors {
  form?: string;
  files: Partial<Record<VerificationDocumentType, string>>;
}

export function VerificationForm() {
  const { submit, isSubmitting } = useVerification();
  const [buffer, setBuffer] = useState<EvidenceBuffer>({});
  const [errors, setErrors] = useState<FieldErrors>({ files: {} });

  const setFile = (documentType: VerificationDocumentType, file?: File) =>
    setBuffer((current) => ({ ...current, [documentType]: file }));

  const onSubmit = () => {
    // Same iteration order `useVerification.submit` builds the zod `documents`
    // array in (`Object.entries(buffer)`), so a per-file issue at `documents[i]`
    // maps back to the field the user actually attached — not the catalog order.
    const attached = (Object.keys(buffer) as VerificationDocumentType[]).filter(
      (type) => buffer[type],
    );
    const parsed = submit(buffer, () => {
      setBuffer({});
      setErrors({ files: {} });
    });

    if (parsed.success) return;

    const next: FieldErrors = { files: {} };
    for (const issue of parsed.error.issues) {
      if (issue.path[0] === "documents" && typeof issue.path[1] === "number") {
        next.files[attached[issue.path[1]]] = issue.message;
      } else {
        next.form = issue.message;
      }
    }
    setErrors(next);
  };

  return (
    <div className="space-y-4">
      {DOCUMENT_TYPES.map((documentType) => (
        <div key={documentType} className="space-y-1.5">
          <Label htmlFor={`evidence-${documentType}`}>
            {VERIFICATION_DOCUMENT_TYPE_LABELS[documentType]}
          </Label>
          <Input
            id={`evidence-${documentType}`}
            data-testid={`evidence-${documentType}`}
            type="file"
            accept={ACCEPT}
            onChange={(event) =>
              setFile(documentType, event.target.files?.[0] ?? undefined)
            }
          />
          {errors.files[documentType] && (
            <p className="text-destructive text-sm">
              {errors.files[documentType]}
            </p>
          )}
        </div>
      ))}

      {errors.form && (
        <p className="text-destructive text-sm">{errors.form}</p>
      )}

      <Button
        onClick={onSubmit}
        disabled={isSubmitting}
        data-testid="submit-verification"
      >
        {isSubmitting ? "Submitting..." : "Submit evidence"}
      </Button>
    </div>
  );
}
