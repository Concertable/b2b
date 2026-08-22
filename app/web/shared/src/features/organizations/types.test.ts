import { describe, expect, it } from "vitest";
import {
  Organization,
  OrganizationBuffer,
  type Organization as OrganizationValue,
  type OrganizationBuffer as OrganizationBufferValue,
} from "./types";

const buffer: OrganizationBufferValue = {
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
};

describe("Organization", () => {
  it("creates an empty editable buffer when tax compliance is absent", () => {
    expect(
      Organization.toBuffer({
        id: "organization-1",
        legalName: "Concertable Ltd",
      }),
    ).toEqual({
      legalName: "Concertable Ltd",
      vatRegistered: false,
      vatNumber: "",
      sellerIdentifier: "",
      line1: "",
      line2: "",
      city: "",
      postcode: "",
      country: "United Kingdom",
      bankReference: "",
      holdsMusicLicence: false,
    });
  });

  it("preserves an existing VAT registration and address", () => {
    const organization: OrganizationValue = {
      id: "organization-1",
      legalName: "Concertable Ltd",
      taxCompliance: {
        vatNumber: "GB123",
        sellerIdentifier: "GB-123",
        registeredAddress: {
          line1: "1 Music Street",
          line2: "Suite 2",
          city: "London",
          postcode: "SW1A 1AA",
          country: "United Kingdom",
        },
        bankReference: "GB00 TEST",
        holdsMusicLicence: true,
      },
    };

    expect(Organization.toBuffer(organization)).toEqual({
      ...buffer,
      vatRegistered: true,
      vatNumber: "GB123",
      line2: "Suite 2",
    });
  });
});

describe("OrganizationBuffer.toUpdateRequest", () => {
  it("builds the nested request and omits inactive optional values", () => {
    expect(OrganizationBuffer.toUpdateRequest(buffer)).toEqual({
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

  it("includes VAT and address line two when supplied", () => {
    const request = OrganizationBuffer.toUpdateRequest({
      ...buffer,
      vatRegistered: true,
      vatNumber: "GB123",
      line2: "Suite 2",
    });

    expect(request.taxCompliance.vatNumber).toBe("GB123");
    expect(request.taxCompliance.registeredAddress.line2).toBe("Suite 2");
  });
});
