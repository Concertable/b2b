import { z } from "zod";

// No backend ResolveReportRequestValidator exists to mirror bounds against — outcome is the only
// required field, notes stays free-form.
export const resolveReportRequestSchema = z.object({
  outcome: z.enum(["noActionTaken", "contentRemoved", "referredToLegal"]),
  notes: z.string().optional(),
});

export type ResolveReportRequest = z.infer<typeof resolveReportRequestSchema>;
