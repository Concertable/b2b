import { describe, expect, it } from "vitest";
import { Organization, type Organization as OrganizationRead } from "./types";

describe("Organization", () => {
  it("initializes empty form values when tax compliance is absent", () => {
    expect(
      Organization.toFormValues({
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

  it("preserves existing tax and address values", () => {
    const organization: OrganizationRead = {
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

    expect(Organization.toFormValues(organization)).toEqual({
      legalName: "Concertable Ltd",
      vatRegistered: true,
      vatNumber: "GB123",
      sellerIdentifier: "GB-123",
      line1: "1 Music Street",
      line2: "Suite 2",
      city: "London",
      postcode: "SW1A 1AA",
      country: "United Kingdom",
      bankReference: "GB00 TEST",
      holdsMusicLicence: true,
    });
  });
});
