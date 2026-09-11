import { z } from "zod";
import type { UpdateOrganizationRequest } from "../types";

export const updateOrganizationRequestSchema = z
  .object({
    legalName: z
      .string()
      .trim()
      .min(1, "Legal name is required")
      .max(200, "Legal name must be 200 characters or fewer"),
    vatRegistered: z.boolean(),
    vatNumber: z
      .string()
      .trim()
      .max(20, "VAT number must be 20 characters or fewer"),
    sellerIdentifier: z
      .string()
      .trim()
      .min(1, "Company or tax reference is required")
      .max(50, "Company or tax reference must be 50 characters or fewer"),
    line1: z
      .string()
      .trim()
      .min(1, "Address line 1 is required")
      .max(200, "Address line 1 must be 200 characters or fewer"),
    line2: z
      .string()
      .trim()
      .max(200, "Address line 2 must be 200 characters or fewer"),
    city: z
      .string()
      .trim()
      .min(1, "City is required")
      .max(100, "City must be 100 characters or fewer"),
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
    bankReference: z
      .string()
      .trim()
      .min(1, "Bank reference is required")
      .max(50, "Bank reference must be 50 characters or fewer"),
    holdsMusicLicence: z.boolean(),
  })
  .superRefine((values, context) => {
    if (values.vatRegistered && values.vatNumber.length === 0)
      context.addIssue({
        code: "custom",
        path: ["vatNumber"],
        message: "Enter your VAT number",
      });
  })
  .transform(
    (values): UpdateOrganizationRequest => ({
      legalName: values.legalName,
      taxCompliance: {
        vatNumber: values.vatRegistered ? values.vatNumber : undefined,
        sellerIdentifier: values.sellerIdentifier,
        registeredAddress: {
          line1: values.line1,
          line2: values.line2 || undefined,
          city: values.city,
          postcode: values.postcode,
          country: values.country,
        },
        bankReference: values.bankReference,
        holdsMusicLicence: values.holdsMusicLicence,
      },
    }),
  );
