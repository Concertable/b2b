export interface RegisteredAddress {
  line1: string;
  line2?: string | null;
  city: string;
  postcode: string;
  country: string;
}

export interface TaxCompliance {
  // Absent = not VAT-registered; the presence of a number is the registration status.
  vatNumber?: string;
  sellerIdentifier: string;
  registeredAddress: RegisteredAddress;
  bankReference: string;
}

export interface TaxFormLabels {
  sellerIdentifierLabel: string;
  sellerIdentifierHint: string;
  vatLabel: string;
  vatNumberPlaceholder: string;
}

export interface Organization {
  id: string;
  legalName: string;
  // Stored tax details (form pre-fill); null until organization setup is completed.
  taxCompliance: TaxCompliance | null;
  // Derived nag flag — the same completeness rule the payout gate consumes.
  taxComplete: boolean;
  // Region field labels the form renders (region config, not per-tenant data).
  formLabels: TaxFormLabels;
}

export interface UpdateOrganizationRequest {
  legalName: string;
  taxCompliance: TaxCompliance;
}
