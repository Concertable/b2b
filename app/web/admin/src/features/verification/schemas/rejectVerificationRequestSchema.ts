import { z } from "zod";

// Mirrors the backend RejectVerificationRequest — a reason is required so the tenant knows what to fix.
export const rejectVerificationRequestSchema = z.object({
  reason: z.string().trim().min(1, "A reason is required."),
});

export type RejectVerificationRequest = z.infer<
  typeof rejectVerificationRequestSchema
>;
export type RejectVerificationFormValues = z.input<
  typeof rejectVerificationRequestSchema
>;
