import { useState } from "react";
import {
  eSignatureRequestSchema,
  type ESignatureRequest,
} from "@concertable/shared/features/concerts/schemas/eSignatureRequestSchema";

export function useESignature() {
  const [signature, setSignature] = useState<ESignatureRequest>({ signatoryName: "" });
  const isValid = eSignatureRequestSchema.safeParse(signature).success;
  return { signature, setSignature, isValid };
}
