import { describe, expect, it } from "vitest";
import { updateOrganizationRequestSchema } from "./updateOrganizationRequestSchema";

const validForm = {
  legalName: " Concertable Ltd ",
  vatRegistered: false,
  vatNumber: " ",
  sellerIdentifier: " GB-123 ",
  line1: " 1 Music Street ",
  line2: " ",
  city: " London ",
  postcode: " SW1A 1AA ",
  country: " United Kingdom ",
  bankReference: " GB00 TEST ",
  holdsMusicLicence: true,
};

describe("updateOrganizationRequestSchema", () => {
  it("normalizes form values into the nested request", () => {
    expect(updateOrganizationRequestSchema.parse(validForm)).toEqual({
      legalName: "Concertable Ltd",
      taxCompliance: {
        vatNumber: undefined,
        sellerIdentifier: "GB-123",
        registeredAddress: {
          line1: "1 Music Street",
          line2: undefined,
          city: "London",
          postcode: "SW1A 1AA",
          country: "United Kingdom",
        },
        bankReference: "GB00 TEST",
        holdsMusicLicence: true,
      },
    });
  });

  it("requires a VAT number only when VAT registration is selected", () => {
    const parsed = updateOrganizationRequestSchema.safeParse({
      ...validForm,
      vatRegistered: true,
    });

    expect(parsed.success).toBe(false);
    if (!parsed.success)
      expect(parsed.error.issues[0]).toMatchObject({
        path: ["vatNumber"],
        message: "Enter your VAT number",
      });
  });

  it("includes active optional values", () => {
    const request = updateOrganizationRequestSchema.parse({
      ...validForm,
      vatRegistered: true,
      vatNumber: " GB123 ",
      line2: " Suite 2 ",
    });

    expect(request.taxCompliance.vatNumber).toBe("GB123");
    expect(request.taxCompliance.registeredAddress.line2).toBe("Suite 2");
  });
});
