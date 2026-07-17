export interface RegisteredAddress {
  line1: string;
  line2?: string;
  city: string;
  postcode: string;
  country: string;
}

// One structure for both read and write. Absent VAT number = not VAT-registered (a valid, complete state);
// every other field is required, so a present TaxCompliance is always complete.
export interface TaxCompliance {
  vatNumber?: string;
  sellerIdentifier: string;
  registeredAddress: RegisteredAddress;
  bankReference: string;
}

export interface Organization {
  id: string;
  legalName: string;
  // Absent until setup — its presence IS completeness (the API rejects incomplete/invalid data on write).
  taxCompliance?: TaxCompliance;
}

export interface UpdateOrganizationRequest {
  legalName: string;
  taxCompliance: TaxCompliance;
}
