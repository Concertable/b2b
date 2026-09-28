import { z } from "zod";

export const reportCategories = [
  "illegalContent",
  "harassment",
  "fraud",
  "spam",
  "other",
] as const;

export const reportCategoryLabels = {
  illegalContent: "Illegal content",
  harassment: "Harassment or abuse",
  fraud: "Fraud or scam",
  spam: "Spam",
  other: "Something else",
} satisfies Record<(typeof reportCategories)[number], string>;

export const reportMessageRequestSchema = z.object({
  category: z.enum(reportCategories),
  details: z
    .string()
    .trim()
    .max(2000, "Details must be 2000 characters or fewer")
    .transform((details) => details || undefined)
    .optional(),
});
