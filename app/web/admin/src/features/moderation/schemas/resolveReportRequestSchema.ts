import { z } from "zod";

// No backend ResolveReportRequestValidator exists to mirror bounds against — outcome is the only
// required field, notes stays free-form. The trim/empty-to-undefined normalization lives here (not a
// facade hook) because react-hook-form's zodResolver owns the raw-values-to-request mapping.
export const resolveReportRequestSchema = z.object({
  outcome: z.enum(["noActionTaken", "contentRemoved", "referredToLegal"]),
  notes: z
    .string()
    .trim()
    .transform((v) => v || undefined)
    .optional(),
});

export type ResolveReportRequest = z.infer<typeof resolveReportRequestSchema>;
export type ResolveReportFormValues = z.input<typeof resolveReportRequestSchema>;
