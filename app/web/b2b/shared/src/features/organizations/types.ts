export interface RegisteredAddress {
  line1: string;
  line2?: string | null;
  city: string;
  postcode: string;
  country: string;
}

export interface Compliance {
  // Absent = not VAT-registered; the presence of a number is the registration status.
  vatNumber?: string;
  sellerIdentifier: string;
  registeredAddress: RegisteredAddress;
  bankReference: string;
}

export interface Dac7 {
  complete: boolean;
  sellerIdentifierLabel: string;
  sellerIdentifierHint: string;
  vatLabel: string;
  vatNumberPlaceholder: string;
}

export interface Organization {
  id: string;
  legalName: string;
  compliance: Compliance | null;
  dac7: Dac7;
}

export interface UpdateOrganizationRequest {
  legalName: string;
  compliance: Compliance;
}
