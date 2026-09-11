import { z } from "zod";

// Client-side affordance mirroring the backend VerificationDocumentFileValidator — the server re-checks
// content type, size and magic bytes on every file regardless.
const ACCEPTED_CONTENT_TYPES = ["application/pdf", "image/jpeg", "image/png"];
const MAX_FILE_SIZE = 10 * 1024 * 1024;

const evidenceUploadSchema = z.object({
  documentType: z.enum(["licence", "proofOfAddress", "companyRegistration"]),
  file: z
    .instanceof(File)
    .refine(
      (file) => ACCEPTED_CONTENT_TYPES.includes(file.type),
      "Evidence must be a PDF, JPEG or PNG file.",
    )
    .refine(
      (file) => file.size > 0 && file.size <= MAX_FILE_SIZE,
      "Evidence file exceeds the maximum size of 10MB.",
    ),
});

export const submitVerificationRequestSchema = z.object({
  documents: z
    .array(evidenceUploadSchema)
    .min(1, "Attach at least one document."),
});

export type SubmitVerificationRequest = z.infer<
  typeof submitVerificationRequestSchema
>;
export type EvidenceUpload = SubmitVerificationRequest["documents"][number];
