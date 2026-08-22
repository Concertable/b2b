import { describe, expect, it } from "vitest";
import { updateOrganizationRequestSchema } from "./updateOrganizationRequestSchema";

const validBuffer = {
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
  it("normalizes every text field before request conversion", () => {
    expect(updateOrganizationRequestSchema.parse(validBuffer)).toEqual({
      legalName: "Concertable Ltd",
      vatRegistered: false,
      vatNumber: "",
      sellerIdentifier: "GB-123",
      line1: "1 Music Street",
      line2: "",
      city: "London",
      postcode: "SW1A 1AA",
      country: "United Kingdom",
      bankReference: "GB00 TEST",
      holdsMusicLicence: true,
    });
  });

  it("requires a VAT number only when VAT registration is selected", () => {
    const parsed = updateOrganizationRequestSchema.safeParse({
      ...validBuffer,
      vatRegistered: true,
    });

    expect(parsed.success).toBe(false);
    if (!parsed.success)
      expect(parsed.error.issues[0]).toMatchObject({
        path: ["vatNumber"],
        message: "Enter your VAT number",
      });
  });
});
