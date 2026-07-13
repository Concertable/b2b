import { useState } from "react";
import {
  eSignatureRequestSchema,
  type ESignatureRequest,
} from "@concertable/shared/features/concerts/schemas/eSignatureRequestSchema";

/* Owns the signature buffer and its validity for the apply/accept flows. `isValid` gates the submit
   button; the ESignaturePanel renders the per-field message. */
export function useESignature() {
  const [signature, setSignature] = useState<ESignatureRequest>({ signatoryName: "" });
  const isValid = eSignatureRequestSchema.safeParse(signature).success;
  return { signature, setSignature, isValid };
}
