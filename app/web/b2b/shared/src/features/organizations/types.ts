export interface RegisteredAddress {
  line1: string;
  line2?: string;
  city: string;
  postcode: string;
  country: string;
}

export interface TaxCompliance {
  vatNumber?: string;
  sellerIdentifier: string;
  registeredAddress: RegisteredAddress;
  bankReference: string;
  holdsMusicLicence: boolean;
}

export interface Organization {
  id: string;
  legalName: string;
  taxCompliance?: TaxCompliance;
}

export interface OrganizationFormValues {
  legalName: string;
  vatRegistered: boolean;
  vatNumber: string;
  sellerIdentifier: string;
  line1: string;
  line2: string;
  city: string;
  postcode: string;
  country: string;
  bankReference: string;
  holdsMusicLicence: boolean;
}

export interface UpdateOrganizationRequest {
  legalName: string;
  taxCompliance: TaxCompliance;
}

export const Organization = {
  toFormValues(organization: Organization): OrganizationFormValues {
    const tax = organization.taxCompliance;
    return {
      legalName: organization.legalName,
      vatRegistered: tax?.vatNumber !== undefined,
      vatNumber: tax?.vatNumber ?? "",
      sellerIdentifier: tax?.sellerIdentifier ?? "",
      line1: tax?.registeredAddress.line1 ?? "",
      line2: tax?.registeredAddress.line2 ?? "",
      city: tax?.registeredAddress.city ?? "",
      postcode: tax?.registeredAddress.postcode ?? "",
      country: tax?.registeredAddress.country ?? "United Kingdom",
      bankReference: tax?.bankReference ?? "",
      holdsMusicLicence: tax?.holdsMusicLicence ?? false,
    };
  },
};
