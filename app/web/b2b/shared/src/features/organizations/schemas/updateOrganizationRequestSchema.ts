import { z } from "zod";

// Bounds mirror the backend UpdateOrganizationRequestValidator — keep them in sync.
const registeredAddressSchema = z.object({
  line1: z
    .string()
    .trim()
    .min(1, "Address line 1 is required")
    .max(200, "Address line 1 must be 200 characters or fewer"),
  line2: z.string().trim().max(200, "Address line 2 must be 200 characters or fewer").optional(),
  city: z.string().trim().min(1, "City is required").max(100, "City must be 100 characters or fewer"),
  postcode: z
    .string()
    .trim()
    .min(1, "Postcode is required")
    .max(20, "Postcode must be 20 characters or fewer"),
  country: z
    .string()
    .trim()
    .min(1, "Country is required")
    .max(100, "Country must be 100 characters or fewer"),
});

const taxComplianceSchema = z.object({
  vatNumber: z.string().trim().min(1, "Enter your VAT number").max(20, "VAT number must be 20 characters or fewer").optional(),
  sellerIdentifier: z
    .string()
    .trim()
    .min(1, "Company or tax reference is required")
    .max(50, "Company or tax reference must be 50 characters or fewer"),
  registeredAddress: registeredAddressSchema,
  bankReference: z
    .string()
    .trim()
    .min(1, "Bank reference is required")
    .max(50, "Bank reference must be 50 characters or fewer"),
});

export const updateOrganizationRequestSchema = z.object({
  legalName: z
    .string()
    .trim()
    .min(1, "Legal name is required")
    .max(200, "Legal name must be 200 characters or fewer"),
  taxCompliance: taxComplianceSchema,
});

export type UpdateOrganizationRequest = z.infer<typeof updateOrganizationRequestSchema>;
