export interface RegisteredAddress {
  line1: string;
  line2?: string | null;
  city: string;
  postcode: string;
  country: string;
}

export interface Compliance {
  vatRegistered: boolean;
  vatNumber?: string | null;
  sellerIdentifier: string;
  registeredAddress: RegisteredAddress;
  bankReference: string;
}

export interface Organization {
  id: string;
  legalName: string;
  compliance: Compliance | null;
}

export interface UpdateOrganizationRequest {
  legalName: string;
  compliance: Compliance;
}
